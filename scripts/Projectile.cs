using Enums;
using Godot;
using System;
using static Enums.EnemyType;

public partial class Projectile : Area2D
{
	[Signal]
	public delegate void projectileHitAreaEventHandler(Area2D area);
	[Signal]
	public delegate void projectileHitPlayerEventHandler();
	[Export]
	public float speed = 200f;
	public float angularSpeed = 500f;
	private const int offset = 15;
	private Vector2 initialPosition { get; set; }
	private float initialRotation { get; set; }
	public Vector2 velocity { get; set; } = Vector2.Zero;
	[Export]
	public bool isProjectile { get; set; }
	public Node2D owner;
	public Sprite2D sprite;

	public void init(Vector2 position, Vector2 vel, float rotation, Node2D owner, float s, float angularS, bool isProjectile, Texture2D texture)
	{
		this.owner = owner;
		velocity = vel;
		// Sets the bullet away from the entity
		Position = position + velocity * offset;
		Rotation = rotation;
		speed = s;
		angularSpeed = angularS;
		this.isProjectile = isProjectile;
		sprite = GetNode<Sprite2D>("ProjectileSprite");
		sprite.Texture = texture;
		SetPhysicsProcess(true);
		Show();

	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (IsPhysicsProcessing())
		{
			Position += velocity * speed * (float)delta;
			if (!isProjectile)
			{
				Rotation += angularSpeed * (float)delta;
			}
		}
	}

	//Hitting enemy
	private void onAreaDetected(Area2D area)
	{
		if (owner is Player o && area.GetParent() is Enemy e)
		{
			EmitSignal(SignalName.projectileHitArea, area);
			projectileHitArea -= o.onRangedEnemyHit;
			PoolEngine.instance.addToPool(this);
		}
	}

	private void onBodyEntered(Node2D body)
	{
		//Player collision
		if (owner is RangedEnemy o && body is Player p)
		{
			EmitSignal(SignalName.projectileHitPlayer);
			projectileHitPlayer -= o.onPlayerHit;
			playHitSound(o.type);
			PoolEngine.instance.addToPool(this);

		}
		//Wall Collision from Enemy
		else if (owner is RangedEnemy own)
		{
			projectileHitPlayer -= own.onPlayerHit;
			playHitSound(own.type);
			PoolEngine.instance.addToPool(this);

		}
		//Wall Collision from Player
		else
		{
			SetPhysicsProcess(false);
			AssetManager.instance.playSFX("rangedWallHit", -5f);
		}

	}

	private void playHitSound(EnemyType type)
	{
		switch (type)
		{
			case BAT:
				AssetManager.instance.playSFX("batAttackHit");
				break;
			default:
				GD.PrintErr("No wall hit sound available for projectile");
				break;
		}
	}

}
