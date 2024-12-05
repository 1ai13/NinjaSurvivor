using Godot;
using System;
using System.Linq;

public abstract partial class Enemy : Node2D
{
	[Export]
	protected EnemyData data;
	public string name;
	public float speed;
	public float attackRange;
	protected int health { get; set; }
	public int damage;
	protected Player player;
	public Vector2 enemyDirection { get; set; }
	public AnimationPlayer animation;
	protected Sprite2D enemySprite;
	protected Timer attackCooldown;
	private Timer hitCooldown;
	private ProgressBar baseHealthBar;
	private ProgressBar healthBar;
	private PackedScene healthBarLabel;
	private Vector2I healthBarOffset = new Vector2I(8, 14);
	private Vector2I healthBarLabelOffset = new Vector2I(0, 6);
	private float lerpValue = 0f;
	private const float lerpDuration = .5f;
	private int lastHealth;
	private bool isDead = false;
	public AudioStream deadSound;
	protected bool isAttacking = false;
	protected abstract void performAttack();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetParent().GetNode<Player>("Player");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		enemySprite = GetNode<Sprite2D>("EnemyArea/EnemySprite");
		attackCooldown = GetNode<Timer>("AttackCooldown");
		hitCooldown = GetNode<Timer>("HitCooldown");
		baseHealthBar = GetNode<ProgressBar>("BaseHealthBar");
		healthBar = GetNode<ProgressBar>("BaseHealthBar/HealthBar");
		healthBarLabel = AssetManager.instance.enemyHealthBarLabel;
		baseHealthBar.Value = health;
		baseHealthBar.MaxValue = health;
		healthBar.MaxValue = health;
		healthBar.Value = health;
		healthBar.SelfModulate = Colors.Green;
		lastHealth = health;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (lerpValue < lerpDuration && baseHealthBar.Value > 0)
		{
			lerpValue += (float)delta;
			var w = lerpValue / lerpDuration; //Normalizing lerpValue to achieve lerpDuration
			baseHealthBar.Value = Mathf.Lerp(lastHealth, health, w);
			if (baseHealthBar.Value == 0)
			{
				AssetManager.instance.playSFX(deadSound);
				playDeadSound();
				baseHealthBar.Visible = false;
			}
		}
		if (isDead)
		{
			return;
		}
		baseHealthBar.Position = Position - healthBarOffset;
		var distanceToPlayer = player.Position - Position;
		enemyDirection = distanceToPlayer.Normalized();

		if (distanceToPlayer.Length() > attackRange && hitCooldown.IsStopped() && (this is RangedEnemy && !isAttacking))
		{
			Position += enemyDirection * speed * (float)delta;
			EntityHelper.playAnimation(this, "walk");
		}
		else if (distanceToPlayer.Length() <= attackRange && attackCooldown.IsStopped())
		{
			performAttack();
		}

	}

	public void takeDamage(int damage, bool criticalHit)
	{
		GD.Print("damage: " + damage);
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
			isDead = true;
			animation.Play("dead");
			var area = GetNode<Area2D>("EnemyArea");
			area.SetDeferred(Area2D.PropertyName.Monitorable, false);
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
		//Create and animate the HealthBar Labels
		////Creating
		var label = healthBarLabel.Instantiate<Label>();
		label.Position -= healthBarLabelOffset;
		label.Text = "-" + damage.ToString();
		var offset = healthBarLabelOffset;
		var rndX = EntityHelper.rnd.RandiRange(offset.X - 2, offset.X + 2);
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
		////Animating
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
		switch (name)
		{
			//TODO add monster sounds create Monsters Enum and switch the enemies dead sounds
			default:
				GD.PrintErr("No sound available for this enemy");
				break;
		}
	}
}
