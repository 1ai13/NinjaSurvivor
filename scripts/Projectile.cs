using Enums;
using Godot;
using System;
using System.Linq;
using static Enums.EnemyType;

public partial class Projectile : Area2D
{
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
	public int wallRicochet;
	public int hitRicochet;
	public RayCast2D rayCast;

	public void init(Vector2 position, Vector2 vel, float rotation, Node2D owner, float s, float angularS, bool isProjectile, string sprite, int wallRicochet, int hitRicochet)
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
		rayCast = GetNode<RayCast2D>("RayCast");
		this.sprite.Animation = sprite;
		this.wallRicochet = wallRicochet;
		this.hitRicochet = hitRicochet;
		if (!IsPhysicsProcessing())
		{
			SetPhysicsProcess(true);
			Show();
		}
		this.sprite.Play();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("ProjectileSprite");
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
				if (wallRicochet > 0)
				{
					velocity = velocity - 2 * velocity.Dot(rayCast.GetCollisionNormal()) * rayCast.GetCollisionNormal();
					wallRicochet--;
					rotateProjectile();
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
		if (owner is Player player && area.GetParent() is Enemy enemy)
		{
			var isCrit = EntityHelper.isCriticalHit(player.criticalChance);
			int dmg;
			if (isCrit)
			{
				dmg = (int)(player.damage.Y * player.criticalDamage);
			}
			else
			{
				dmg = EntityHelper.getVariableDamage((int)player.damage.Y);
			}
			enemy.takeDamage(dmg, isCrit);

			//Enemy hit ricochet
			if (hitRicochet > 0)
			{
				//Querying all enemies
				var enemies = GetTree().GetNodesInGroup("Enemies");
				var nearestDirection = Vector2.Inf;
				foreach (var e in enemies.Cast<Enemy>())
				{
					if (e.isDead)
						continue;

					//Searching for nearest one
					var direction = e.GlobalPosition - GlobalPosition;
					if (nearestDirection.Length() > direction.Length())
					{
						nearestDirection = direction;
					}
				}
				velocity = nearestDirection.Normalized();
				hitRicochet--;
				rotateProjectile();
			}
			else
			{
				PoolEngine.instance.addToPool(this);
			}
		}
	}

	//Area Collisions
	private void onBodyEntered(Node2D body)
	{
		//Player collision
		if (owner is RangedEnemy o && body is Player p)
		{
			p.takeDamage(o.damage);
			playHitSound(o.type);
			PoolEngine.instance.addToPool(this);

		}
		//Wall Collision from Enemy
		else if (owner is RangedEnemy own)
		{
			playHitSound(own.type);
			PoolEngine.instance.addToPool(this);

		}
	}

	private void rotateProjectile()
	{
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

	public void resetProjectile()
	{
		owner = null;
		velocity = Vector2.Zero;
		Position = Vector2.Zero;
		Rotation = 0;
		speed = 0;
		angularSpeed = 0;
		rayCast.Rotation = 0;
		wallRicochet = 0;
		hitRicochet = 0;
		sprite.Stop();
	}

}