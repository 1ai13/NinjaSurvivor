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
	private int offset = 10;
	private Vector2 initialPosition { get; set; }
	private float initialRotation { get; set; }
	public Vector2 velocity { get; set; } = Vector2.Zero;
	[Export]
	public bool isProjectile { get; set; }
	public Node2D owner;
	public AnimatedSprite2D sprite;
	public int ricochets;
	public RayCast2D rayCast;

	public void init(Vector2 position, Vector2 vel, float rotation, Node2D owner, float s, float angularS, bool isProjectile, string sprite, int ricochets)
	{
		this.owner = owner;
		velocity = vel;
		// Sets the bullet away from the entity
		if (owner is Player)
		{
			Position = position + velocity;
		}
		else
		{
			Position = position + velocity * offset;
		}
		Rotation = rotation;
		speed = s;
		angularSpeed = angularS;
		this.isProjectile = isProjectile;
		this.sprite = GetNode<AnimatedSprite2D>("ProjectileSprite");
		rayCast = GetNode<RayCast2D>("RayCastFront");
		this.sprite.Animation = sprite;
		this.ricochets = ricochets;
		SetPhysicsProcess(true);
		Show();
		this.sprite.Play();
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
			//Update movement
			Position += velocity * speed * (float)delta;
			if (!isProjectile)
			{
				Rotation += angularSpeed * (float)delta;
				rayCast.Rotation -= angularSpeed * (float)delta;
			}
			//Check for RayCast collisions
			if (rayCast.IsColliding() && owner is Player)
			{
				//Reflection formula for V and Normal (perpendicular vector against surface) Rv = 2 - (V*N)*N [Vector2.Bounce()]
				if (ricochets > 0)
				{
					velocity = velocity - 2 * velocity.Dot(rayCast.GetCollisionNormal()) * rayCast.GetCollisionNormal();
					ricochets--;
					if (!isProjectile)
					{
						Rotation = 0;
						rayCast.Rotation = velocity.Angle();
					}
					else
					{
						Rotation = velocity.Angle();
					}
				}
				else
				{
					SetPhysicsProcess(false);
				}
				AssetManager.instance.playSFX("rangedWallHit", -5f);
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

	//Area Collisions
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
	}

	private void playHitSound(EnemyType type)
	{
		switch (type)
		{
			case BAT:
				AssetManager.instance.playSFX("batAttackHit");
				break;
			case BAMBOO:
				AssetManager.instance.playSFX("bambooAttackHit");
				break;
			default:
				GD.PrintErr("No wall hit sound available for projectile");
				break;
		}
	}

}
