using Enums;
using Godot;
using System;
using System.Linq;
using static Enums.EnemyType;
using static Enums.ItemType;
public abstract partial class Enemy : Node2D
{
	[Export]
	public EnemyData data;
	public EnemyType type;
	public float speed;
	public float attackRange;
	public int health { get; set; }
	public int damage;
	protected Player player;
	public Vector2 enemyDirection { get; set; }
	public AnimationPlayer animation;
	protected Area2D enemyArea;
	protected Sprite2D enemySprite;
	protected Timer attackCooldown;
	protected Timer hitCooldown;
	protected ProgressBar baseHealthBar;
	private ProgressBar healthBar;
	protected PackedScene healthBarLabel;
	private Vector2I healthBarLabelOffset = new Vector2I(0, 6);
	private float lerpValue = 1f;
	private const float lerpDuration = .5f;
	private int lastHealth;
	public bool isDead = false;
	public AudioStream deadSound;
	protected bool isAttacking = false;
	protected abstract void performAttack();
	protected bool randomDirection;
	protected Vector2 distanceToPlayer;
	protected Vector2 initialHealthBarPos;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetPhysicsProcess(false);
		player = GetParent().GetNode<Player>("Player");
		enemyArea = GetNode<Area2D>("EnemyArea");
		attackCooldown = GetNode<Timer>("AttackCooldown");
		hitCooldown = GetNode<Timer>("HitCooldown");
		baseHealthBar = GetNode<ProgressBar>("EnemyArea/BaseHealthBar");
		initialHealthBarPos = baseHealthBar.Position;
		healthBar = GetNode<ProgressBar>("EnemyArea/BaseHealthBar/HealthBar");
		healthBarLabel = AssetManager.instance.enemyHealthBarLabel;
		enemyArea.BodyEntered += onBodyCollission;
		if (this is not EnemyBoss)
		{
			baseHealthBar.Value = health;
			baseHealthBar.MaxValue = health;
			healthBar.MaxValue = health;
			healthBar.Value = health;
			healthBar.SelfModulate = Colors.Green;
			lastHealth = health;
			enemySprite = GetNode<Sprite2D>("EnemyArea/EnemySprite");
			animation = GetNode<AnimationPlayer>("AnimationPlayer");
			animation.AnimationFinished += onAnimationFinished;
		}
	}

	public override void _Process(double delta)
	{
		//Adjust HealthBar taken damage over time
		if (lerpValue < lerpDuration && baseHealthBar.Value > 0)
		{
			lerpValue += (float)delta;
			var w = lerpValue / lerpDuration; //Capping lerpValue
			baseHealthBar.Value = Mathf.Lerp(lastHealth, health, w);
			if (baseHealthBar.Value == 0)
			{
				baseHealthBar.Visible = false;
				//Create random item
				var rnd = EntityHelper.rnd;
				if (rnd.Randf() >= player.dropLuck)
				{
					float rand = EntityHelper.rnd.Randf() * Mathf.Tau;
					var offset = new Vector2(Mathf.Cos(rand), MathF.Sin(rand) * 10);
					var item = PoolEngine.pool.pullFromPool<Item>();
					if (rnd.Randf() <= .66f)
					{

						item.init(GlobalPosition + offset, COIN, this);
					}
					else
					{
						item.init(GlobalPosition + offset, HEART, this);
					}
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (isDead || player.health == 0)
		{
			SetPhysicsProcess(false);
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
				enemyDirection = EntityHelper.getRandomDirection();
				randomDirection = true;
			}
			Position += enemyDirection * speed * (float)delta;
			EntityHelper.playAnimation(this, "walk");
		}
	}

	public virtual void takeDamage(int damage, bool criticalHit)
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
			animation.Play("dead");
			enemyDead();
		}

		//Animate the enemy flash hit and SFX
		var tweenSprite = CreateTween();
		tweenSprite.TweenProperty(enemySprite, "self_modulate", new Color(4, 4, 4, 4), .2f);
		tweenSprite.TweenProperty(enemySprite, "self_modulate", Colors.White, 0);
		AssetManager.instance.playSFX("enemyHit", -5f);
		hitCooldown.Start();
		animateHealthBar(damage, criticalHit);
	}

	protected void enemyDead()
	{
		isDead = true;
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onEnemyKilled));
		playDeadSound();
		enemyArea.SetDeferred(Area2D.PropertyName.Monitorable, false);
	}

	protected void animateHealthBar(int damage, bool criticalHit)
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

	private void playDeadSound()
	{
		switch (type)
		{
			case SLIME:
				AssetManager.instance.playSFX("deadSlime", 15f);
				break;
			case BAT:
				AssetManager.instance.playSFX("deadBat");
				break;
			case BAMBOO:
				AssetManager.instance.playSFX("deadBamboo");
				break;
			case BAMBOO_BOSS:
				AssetManager.instance.playSFX(GD.Load<AudioStream>("res://assets/audio/enemies/bosses/bamboo/bambooAwake.wav"));
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
			case BAMBOO_BOSS:
				AssetManager.instance.playSFX("bambooAttack", -10f);
				break;
			default:
				GD.PrintErr("Unable to load attack sound from enemy");
				break;
		}
	}

	protected void onBodyCollission(Node2D body)
	{
		if (body is TileMapLayer)
		{
			enemyDirection *= -1;
		}
	}

	protected void setEnemyProcess(bool value)
	{
		SetPhysicsProcess(value);
	}
}
