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
	public bool specialAttack;
	public float fallTime;

	public void init(Vector2 position, Vector2 direction, float rotation, Node2D owner, float speed, float angularS, bool isProjectile, string sprite, int wallRicochet, int hitRicochet)
	{
		this.owner = owner;
		velocity = direction;
		// Sets the bullet away from the entity
		if (owner is Player)
		{
			Position = position + velocity;
		}
		else
		{
			Position = position + velocity * offset;
		}
		initialPosition = Position;
		specialAttack = false;
		fallTime = 0;
		Monitoring = true;
		Rotation = rotation;
		this.speed = speed;
		angularSpeed = angularS;
		this.isProjectile = isProjectile;
		if (owner is EnemyBoss)
		{
			Scale = Vector2.One * 1.25f;
		}
		else
		{
			Scale = Vector2.One;
		}
		rayCast = GetNode<RayCast2D>("RayCast");
		rayCast.Enabled = true;
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
			if (!isProjectile)
			{
				Rotation += angularSpeed * (float)delta;
				rayCast.Rotation -= angularSpeed * (float)delta;
			}
			//Check for RayCast collisions
			if (rayCast.IsColliding() && (owner is Player || owner is EnemyBoss))
			{
				var collider = rayCast.GetCollider();

				//Reflection formula for V and Normal (perpendicular vector against surface) Rv =V- 2 * (V*N)*N [Vector2.Bounce()]
				if (wallRicochet > 0)
				{
					velocity = velocity - 2 * velocity.Dot(rayCast.GetCollisionNormal()) * rayCast.GetCollisionNormal();
					wallRicochet--;
					rotateProjectile();
					playHitSound();
				}
				else
				{
					if (owner is Player)
					{
						SetPhysicsProcess(false);
						playHitSound();
					}
					else if (rayCast.GetCollider() is not CharacterBody2D)
					{
						playHitSound();
						PoolEngine.pool.addToPool(this);
					}
				}
			}
			if (specialAttack)
			{
				Position = initialPosition.Lerp(initialPosition + Vector2.Down * 489, fallTime);
				if (fallTime >= 1)
				{
					if (!Monitoring)
					{
						GetTree().CreateTimer(.025f).Timeout += deleteSpecialProjectile;
						Monitoring = true;
					}
				}
				return;
			}
			//Update movement
			Position += velocity * speed * (float)delta;
		}
	}

	//Hitting enemy
	private void onAreaDetected(Area2D area)
	{
		if (!IsPhysicsProcessing()) return;
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
			if (enemy is EnemyBoss)
			{
				PoolEngine.pool.addToPool(this);
				return;
			}
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
				PoolEngine.pool.addToPool(this);
			}
		}
	}

	//Body Collisions
	private void onBodyEntered(Node2D body)
	{
		//Player collision
		if (owner is Enemy o && body is Player p)
		{
			p.takeDamage(o.damage);
			playHitSound();
			if (!specialAttack)
			{
				PoolEngine.pool.addToPool(this);
			}
		}
		//Wall Collision from Enemy
		else if (owner is Enemy && body is TileMapLayer)
		{
			if (owner is EnemyBoss && wallRicochet > 0 || owner is EnemyBoss && specialAttack) return;
			playHitSound();
			PoolEngine.pool.addToPool(this);
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

	private void playHitSound()
	{
		if (owner is Player)
		{
			AssetManager.instance.playSFX("rangedWallHit");
			return;
		}
		if (owner is Enemy e)
		{
			switch (e.type)
			{
				case BAT:
					AssetManager.instance.playSFX("batAttackHit");
					break;
				case BAMBOO:
				case BAMBOO_BOSS:
					AssetManager.instance.playSFX("bambooAttackHit");
					break;
				default:
					GD.PrintErr("No wall hit sound available for projectile");
					break;
			}
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
		Scale = Vector2.One;
		rayCast.Enabled = true;
		rayCast.Rotation = 0;
		wallRicochet = 0;
		hitRicochet = 0;
		sprite.Stop();
		initialPosition = Vector2.Zero;
		Monitoring = true;
		specialAttack = false;
		fallTime = 0;
	}

	private void deleteSpecialProjectile()
	{
		PoolEngine.pool.addToPool(this);
	}
}