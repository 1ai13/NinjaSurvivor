using Enums;
using Godot;
using Godot.Collections;
using System;
using static Enums.AttackType;
using static Enums.AnimationType;
public partial class EnemyBoss : Enemy
{
	public AnimatedSprite2D bossAnimation;
	private Array<AttackType> attackTypes;
	private AnimationType currentAnimation;
	private Timer pursueTimer;
	private CharacterBody2D shape;
	private bool pursuePlayer;
	private float maxSpeed;
	private CpuParticles2D specialAttackFX;
	private ShapeRenderer shaper;
	private Area2D specialAreaContainer;
	private CollisionShape2D specialAreaShape;
	private Rect2 specialArea;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		EntityHelper.initEnemy(this, data);
		base._Ready();
		shaper = GetNode<ShapeRenderer>("/root/Game/Shaper");
		specialAreaContainer = GetNode<Area2D>("/root/Game/Shaper/Area2D");
		specialAreaShape = GetNode<CollisionShape2D>("/root/Game/Shaper/Area2D/CollisionShape2D");
		specialArea = specialAreaShape.Shape.GetRect();
		bossAnimation = GetNode<AnimatedSprite2D>("EnemyArea/BossSprite");
		specialAttackFX = GetNode<CpuParticles2D>("SpecialAttackFX");
		attackTypes = new Array<AttackType>() { MELEE, NORMAL, SP_ATTACK };
		shape = GetNode<CharacterBody2D>("EnemyArea/CollidableShape");
		pursueTimer = GetNode<Timer>("PursueTimer");
		maxSpeed = speed;
		setAnimation(IDLE);
		SignalBus.bus.onAwakeBoss += awakeBoss;
	}

	public void init(Vector2 pos)
	{
		Position = pos;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		baseHealthBar.Position = Position + initialHealthBarPos;
		distanceToPlayer = player.GlobalPosition - GlobalPosition;
		//Out of range
		if (distanceToPlayer.Length() > attackRange && !isAttacking || pursuePlayer)
		{
			enemyDirection = distanceToPlayer.Normalized();
			setAnimation(WALK);
			if (!pursuePlayer)
			{
				speed = maxSpeed;
			}
			else
			{
				speed += (float)delta;
				if (distanceToPlayer.Length() < 20f)
				{
					pursuePlayer = false;
					setAnimation(CHARGE);
					isAttacking = true;
					AssetManager.instance.playSFX("bambooBossMelee");
				}
			}
			Position += enemyDirection * speed * (float)delta;

		}
		//In range
		else if (distanceToPlayer.Length() <= attackRange && attackCooldown.IsStopped() && !isAttacking)
		{
			performAttack();
			randomDirection = false;
		}//Moving in random direction
		else if (distanceToPlayer.Length() <= attackRange && !attackCooldown.IsStopped() && !isAttacking)
		{
			if (!randomDirection)
			{
				GD.Print("walk random");
				enemyDirection = EntityHelper.getRandomDirection();
				randomDirection = true;
			}
			speed = maxSpeed;
			Position += enemyDirection * speed * (float)delta;
			setAnimation(WALK);
		}
		if (shape.MoveAndSlide())
		{
			shape.GlobalPosition = GlobalPosition;
			enemyDirection = EntityHelper.getRandomDirection();
		}
	}

	private void chooseAttack()
	{
		GD.Print("Chossing attack");
		var attack = attackTypes.PickRandom();
		switch (attack)
		{
			case MELEE:
				if (distanceToPlayer.Length() < 20f)
				{
					setAnimation(CHARGE);
					isAttacking = true;
					playAttackSound(attack, type);
				}
				else
				{
					pursuePlayer = true;
					pursueTimer.Start();
				}
				break;
			case NORMAL:
				setAnimation(ATTACK);
				isAttacking = true;
				break;
			case SP_ATTACK:
				setAnimation(SPECIAL_ATTACK);
				isAttacking = true;
				GD.Print("Special attack");
				GetTree().CreateTimer(1.35f).Timeout += () =>
				{
					specialAttackFX.Emitting = true;
					playAttackSound(attack, type);
				};
				break;
		}
	}

	protected override void performAttack()
	{
		chooseAttack();
	}

	private async void animationFinished()
	{
		switch (currentAnimation)
		{
			case CHARGE:
				player.takeDamage((int)(damage / 1.5f));
				isAttacking = false;
				attackCooldown.Start();
				break;
			case ATTACK:
				Projectile projectile;
				var playerStop = player.Velocity == Vector2.Zero;
				isAttacking = false;
				attackCooldown.Start();
				generateProjectile();
				//Stop to generate diagonal projectiles
				if (playerStop)
				{
					GetTree().CreateTimer(.2f).Timeout += () =>
					{
						generateProjectile();
						GetTree().CreateTimer(.2f).Timeout += () =>
						{
							generateProjectile();
						};
					};
				}
				else
				{
					for (int i = -1; i <= 1; i += 2)
					{
						projectile = PoolEngine.pool.pullFromPool<Projectile>();
						var angle = enemyDirection.Angle() + Mathf.Pi / (i * 10);
						var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
						projectile.init(GlobalPosition, direction, direction.Angle(), this, data.projectileSpeed, data.angularSpeed, data.isProjectile, "Bamboo", 1, 0);
					}
				}
				playAttackSound();
				break;
			case SPECIAL_ATTACK:
				isAttacking = false;
				attackCooldown.Start();
				var rndPos = Vector2.Zero;
				for (int i = 0; i < 10; i++)
				{
					var attempts = 25;
					for (int j = attempts; j >= 1; j--)
					{
						var offsetX = EntityHelper.rnd.RandfRange(-1, 1) * 35;
						var offsetY = EntityHelper.rnd.RandfRange(-1, 1) * 35;
						rndPos = new Vector2(offsetX, offsetY);
						rndPos = player.GlobalPosition + rndPos;
						if (specialAreaContainer.GlobalPosition.X < rndPos.X && specialAreaContainer.GlobalPosition.Y < rndPos.Y && specialAreaContainer.GlobalPosition.X + specialArea.Size.X > rndPos.X && specialAreaContainer.GlobalPosition.Y + specialArea.Size.Y > rndPos.Y)
						{
							shaper.addCircle(rndPos, this);
							break;
						}
					}
					if (isDead)
					{
						break;
					}
					await ToSignal(GetTree().CreateTimer(.5), "timeout");
				}
				break;
			case HIT:
				var scroll = PoolEngine.pool.pullFromPool<Item>();
				scroll.init(GlobalPosition, ItemType.PLANT_SCROLL, this);
				var tween = CreateTween().SetParallel(true);
				tween.TweenProperty(this, "scale", Vector2.Zero, .5f);
				tween.TweenProperty(this, "modulate", Color.Color8(1, 1, 1, 0), .5f);

				int coinCount = 15;
				float radius = 25;
				for (int i = 0; i < coinCount; i++)
				{
					// Generate a random point within a circle
					float angle = i * Mathf.Tau / coinCount;
					Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
					// Spawn the coin
					var coin = PoolEngine.pool.pullFromPool<Item>();
					coin.init(GlobalPosition + offset, ItemType.COIN, this);
				}
				break;
		}
	}

	private void generateProjectile()
	{
		var projectile = PoolEngine.pool.pullFromPool<Projectile>();
		enemyDirection = distanceToPlayer.Normalized();
		projectile.init(GlobalPosition, enemyDirection, enemyDirection.Angle(), this, data.projectileSpeed, data.angularSpeed, data.isProjectile, "Bamboo", 1, 0);
	}

	private async void awakeBoss()
	{
		//Wake up boss
		setAnimation(AWAKE);
		AssetManager.instance.playSFX("deadBamboo", -5f);
		AssetManager.instance.playSFX(GD.Load<AudioStream>("res://assets/audio/enemies/bosses/bamboo/bambooAwake.wav"), 10f);
		//Span for resetting camera
		await ToSignal(GetTree().CreateTimer(1), "timeout");
		setAnimation(IDLE);
		var tween = CreateTween();
		tween.TweenProperty(player.camera, "offset", Vector2.Zero, .5f);
		await ToSignal(tween, "finished");
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onBossReady), true, health);
		player.setPlayerProcess(true);
		SetPhysicsProcess(true);
	}

	private void setAnimation(AnimationType type)
	{
		bossAnimation.Play(type.ToString().ToLower());
		currentAnimation = type;

	}

	public override void takeDamage(int damage, bool criticalHit)
	{
		if (isDead)
		{
			return;
		}
		health = Math.Max(health - damage, 0);
		if (health == 0)
		{
			SetPhysicsProcess(false);
			setAnimation(HIT);
			enemyDead();
		}
		if (!isDead)
		{
			var lastSpeed = speed;
			var tween = CreateTween();
			speed = maxSpeed / 1.5f;
			tween.TweenProperty(bossAnimation, "self_modulate", Color.Color8(1, 0, 0, 75), .15f);
			tween.TweenProperty(bossAnimation, "self_modulate", Colors.White, 0f);
			tween.Finished += () => speed = lastSpeed;
		}
		AssetManager.instance.playSFX("enemyHit");
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onBossHit), health);
		animateHealthBar(damage, criticalHit);
	}

	private void pursueTimeout()
	{
		pursuePlayer = false;
		if (IsPhysicsProcessing())
		{
			performAttack();
		}
	}

	private void playAttackSound(AttackType type, EnemyType enemyType)
	{
		switch (type)
		{
			case MELEE:
				GD.Print("Playing melee");
				AssetManager.instance.playSFX("bambooBossMelee");
				break;
			case SP_ATTACK:
				AssetManager.instance.playSFX("bambooBossSpecial");
				GetTree().CreateTimer(.25f).Timeout += () =>
				{
					AssetManager.instance.playSFX("bambooBossSpecial");
					GetTree().CreateTimer(.25f).Timeout += () =>
				{
					AssetManager.instance.playSFX("bambooBossSpecial");
				};
				};
				break;
		}
	}
}
namespace Enums
{
	enum AnimationType
	{
		IDLE,
		AWAKE,
		HIT,
		WALK,
		ATTACK,
		SPECIAL_ATTACK,
		CHARGE
	}
}