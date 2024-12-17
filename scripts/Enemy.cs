using Enums;
using Godot;
using System;
using System.Linq;
using static Enums.EnemyType;

public abstract partial class Enemy : Node2D
{
	[Export]
	protected EnemyData data;
	public EnemyType type;
	public float speed;
	public float attackRange;
	protected int health { get; set; }
	public int damage;
	protected Player player;
	public Vector2 enemyDirection { get; set; }
	public AnimationPlayer animation;
	private Area2D enemyArea;
	protected Sprite2D enemySprite;
	protected Timer attackCooldown;
	private Timer hitCooldown;
	private ProgressBar baseHealthBar;
	private ProgressBar healthBar;
	private PackedScene healthBarLabel;
	private Vector2I healthBarLabelOffset = new Vector2I(0, 6);
	private float lerpValue = 0f;
	private const float lerpDuration = .5f;
	private int lastHealth;
	public bool isDead = false;
	public AudioStream deadSound;
	protected bool isAttacking = false;
	protected abstract void performAttack();
	private bool randomDirection;
	protected Vector2 distanceToPlayer;
	private Vector2 initialHealthBarPos;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetPhysicsProcess(false);
		player = GetParent().GetNode<Player>("Player");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		enemySprite = GetNode<Sprite2D>("EnemyArea/EnemySprite");
		enemyArea = GetNode<Area2D>("EnemyArea");
		attackCooldown = GetNode<Timer>("AttackCooldown");
		hitCooldown = GetNode<Timer>("HitCooldown");
		baseHealthBar = GetNode<ProgressBar>("EnemyArea/BaseHealthBar");
		initialHealthBarPos = baseHealthBar.Position;
		healthBar = GetNode<ProgressBar>("EnemyArea/BaseHealthBar/HealthBar");
		healthBarLabel = AssetManager.instance.enemyHealthBarLabel;
		baseHealthBar.Value = health;
		baseHealthBar.MaxValue = health;
		healthBar.MaxValue = health;
		healthBar.Value = health;
		healthBar.SelfModulate = Colors.Green;
		lastHealth = health;
		animation.AnimationFinished += onAnimationFinished;
		enemyArea.BodyEntered += onBodyCollission;
	}

	protected virtual void onAnimationFinished(StringName animName)
	{
		//Activating enemy
		if (animName.ToString().Equals("spawn"))
		{
			SetPhysicsProcess(true);
			enemyArea.Monitorable = true;
		}
		else if (animName.ToString().StartsWith("attack"))
		{
			if (this is RangedEnemy)
			{
				playAttackSound();
			}
			isAttacking = false;
			attackCooldown.Start();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		//Adjust taken damage over time
		if (lerpValue < lerpDuration && baseHealthBar.Value > 0)
		{
			lerpValue += (float)delta;
			var w = lerpValue / lerpDuration; //Capping lerpValue
			baseHealthBar.Value = Mathf.Lerp(lastHealth, health, w);
			if (baseHealthBar.Value == 0)
			{
				baseHealthBar.Visible = false;
			}
		}
		if (isDead)
		{
			return;
		}

		//Enemy VS Player logic
		baseHealthBar.Position = Position + initialHealthBarPos;
		distanceToPlayer = player.Position - Position;
		//Not in range to attack
		if (distanceToPlayer.Length() > attackRange && hitCooldown.IsStopped() && !isAttacking)
		{
			enemyDirection = distanceToPlayer.Normalized();
			Position += enemyDirection * speed * (float)delta;
			EntityHelper.playAnimation(this, "walk");
		}
		//In range
		else if (distanceToPlayer.Length() <= attackRange && !isAttacking && attackCooldown.IsStopped())
		{
			enemyDirection = distanceToPlayer.Normalized();
			performAttack();
			randomDirection = false;
		}
		//After attack re-position
		else if (distanceToPlayer.Length() <= attackRange && !attackCooldown.IsStopped())
		{
			if (!randomDirection)
			{
				var rndX = EntityHelper.rnd.RandfRange(-1, 1);
				var rndY = EntityHelper.rnd.RandfRange(-1, 1);
				enemyDirection = new Vector2(rndX, rndY);
				randomDirection = true;
			}
			Position += enemyDirection * speed * (float)delta;
			EntityHelper.playAnimation(this, "walk");
		}
	}

	public void takeDamage(int damage, bool criticalHit)
	{
		if (isDead)
		{
			return;
		}
		//Assign Health and HealthBar values
		if (!baseHealthBar.Visible) baseHealthBar.Visible = true;
		lastHealth = (int)baseHealthBar.Value;
		health -= damage;
		health = Math.Max(health, 0);
		healthBar.Value = health;
		lerpValue = 0;

		//Tint healthbar based on missing health
		if (health <= healthBar.MaxValue / 4)
		{
			healthBar.SelfModulate = new Color(1, 0, 0, 1);
		}
		else if (health <= healthBar.MaxValue / 2)
		{
			healthBar.SelfModulate = new Color(1, 1, 0, 1);
		}

		if (health == 0)
		{
			//Enemy dead
			isDead = true;
			animation.Play("dead");
			SignalBus.bus.EmitSignal("onEnemyKilled");
			playDeadSound();
			enemyArea.SetDeferred(Area2D.PropertyName.Monitorable, false);
		}

		//Animate the player flash hit and SFX
		var tweenSprite = CreateTween();
		tweenSprite.TweenProperty(enemySprite, "self_modulate", new Color(4, 4, 4, 4), .2f);
		tweenSprite.TweenProperty(enemySprite, "self_modulate", Colors.White, 0);
		AssetManager.instance.playSFX("enemyHit", -10f);
		hitCooldown.Start();
		animateHealthBar(damage, criticalHit);

	}
	private void animateHealthBar(int damage, bool criticalHit)
	{
		//Creating labels
		var label = healthBarLabel.Instantiate<Label>();
		label.Position -= healthBarLabelOffset;
		label.Text = "-" + damage.ToString();
		var offset = healthBarLabelOffset;
		var rndX = EntityHelper.rnd.RandiRange(offset.X - 3, offset.X + 3);
		var rndY = EntityHelper.rnd.RandiRange(offset.Y - 1, offset.Y);
		offset[0] = rndX;
		if (criticalHit)
		{
			label.SelfModulate = Colors.Red;
			offset[1] = offset.Y + 2;
		}
		else
		{
			offset[1] = rndY;
		}
		baseHealthBar.AddChild(label);
		//Animating labels
		var tweenLabel = CreateTween().SetParallel();
		tweenLabel.TweenProperty(label, "position", label.Position - offset, .3f);
		tweenLabel.TweenProperty(label, "rotation", Mathf.DegToRad(-5), .1f);
		tweenLabel.SetParallel(false);
		tweenLabel.TweenProperty(label, "rotation", Mathf.DegToRad(5 * 2), .2f);
		tweenLabel.TweenProperty(label, "rotation", 0, .1f);
		tweenLabel.TweenProperty(label, "visible", false, .2f);

		//Cleanup unused enemy Labels
		baseHealthBar.GetChildren().OfType<Label>().ToList().ForEach(label =>
		{
			if (label is Label l)
			{
				if (l.Visible == false)
				{
					l.QueueFree();
				}
			}
		});
	}

	private void playDeadSound()
	{
		switch (type)
		{
			case SLIME:
				AssetManager.instance.playSFX("deadSlime", 10f);
				break;
			case BAT:
				AssetManager.instance.playSFX("deadBat");
				break;
			case BAMBOO:
				AssetManager.instance.playSFX("deadBamboo");
				break;
			default:
				GD.PrintErr("No dead sound loaded for enemy");
				break;
		}
	}

	protected void playAttackSound()
	{
		switch (type)
		{
			case SLIME:
				AssetManager.instance.playSFX("slimeAttack");
				break;
			case BAT:
				AssetManager.instance.playSFX("batAttack");
				break;
			case BAMBOO:
				AssetManager.instance.playSFX("bambooAttack", -10f);
				break;
			default:
				GD.PrintErr("Unable to load attack sound from enemy");
				break;
		}
	}

	private void onBodyCollission(Node2D body)
	{
		if (body is TileMapLayer)
		{
			enemyDirection *= -1;
		}
	}
}
