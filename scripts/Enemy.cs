using Godot;
using System;
using System.Linq;

public partial class Enemy : Node2D
{
	private const float speed = 60f;
	private const float attackRange = 18f;
	[Export]
	private int health { get; set; } = 100;
	[Export]
	private int damage = 10;
	private Player player;
	public Vector2 enemyDirection { get; set; }
	public AnimationPlayer animation;
	private Sprite2D enemySprite;
	private Timer attackCooldown;
	private Timer hitCooldown;
	private ProgressBar baseHealthBar;
	private ProgressBar healthBar;
	private PackedScene healthBarLabel;
	private Vector2I healthBarOffset = new Vector2I(8, 14);
	private Vector2I healthBarLabelOffset = new Vector2I(0, 6);
	private float lerpValue = 0f;
	private const float lerpDuration = .5f;
	private int lastHealth;

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
		//TODO add attack animation
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		baseHealthBar.Position = Position - healthBarOffset;
		var distanceToPlayer = player.Position - Position;
		enemyDirection = distanceToPlayer.Normalized();

		if (distanceToPlayer.Length() > attackRange && hitCooldown.IsStopped())
		{
			Position += enemyDirection * speed * (float)delta;
		}
		else if (distanceToPlayer.Length() <= attackRange && attackCooldown.IsStopped())
		{
			player.takeDamage(damage);
			attackCooldown.Start();
		}
		EntityHelper.playAnimation(this, "walk");
		if (lerpValue < lerpDuration)
		{
			lerpValue += (float)delta;
			var w = lerpValue / lerpDuration; //Normalizing lerpValue to achieve lerpDuration
			baseHealthBar.Value = Mathf.Lerp(lastHealth, health, w);
		}
	}

	public void takeDamage(int damage, bool criticalHit)
	{
		//Assign Health and HealthBar values
		animation.Pause();
		lastHealth = (int)baseHealthBar.Value;
		health -= damage;
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

		if (health < 0)
		{
			//#TODO enemy die logic
		}

		//Animate the player flash hit
		var tweenSprite = CreateTween();
		tweenSprite.TweenProperty(enemySprite, "self_modulate", new Color(4, 4, 4, 4), .2f);
		tweenSprite.TweenProperty(enemySprite, "self_modulate", Colors.White, 0);
		AssetManager.instance.playSFX("enemyHit", -10f);

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
		var tweenLabel = CreateTween().SetParallel();
		tweenLabel.TweenProperty(label, "position", label.Position - offset, .3f);
		tweenLabel.TweenProperty(label, "rotation", Mathf.DegToRad(-5), .1f);
		tweenLabel.SetParallel(false);
		tweenLabel.TweenProperty(label, "rotation", Mathf.DegToRad(5 * 2), .2f);
		tweenLabel.TweenProperty(label, "rotation", 0, .1f);
		tweenLabel.TweenProperty(label, "visible", false, .2f);
		hitCooldown.Start();

		//Cleanup unused enemy Labels
		baseHealthBar.GetChildren().OfType<Label>().ToList().ForEach(label =>
		{
			GD.Print("Finding labels" + label.Name);
			if (label is Label l)
			{
				if (l.Visible == false)
				{
					GD.Print("Removing labels" + label.Name);
					l.QueueFree();
				}
			}
		});
	}
}
