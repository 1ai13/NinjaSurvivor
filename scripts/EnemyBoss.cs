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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		EntityHelper.initEnemy(this, data);
		base._Ready();
		bossAnimation = GetNode<AnimatedSprite2D>("EnemyArea/BossSprite");
		attackTypes = new Array<AttackType>() { MELEE };
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
				}
				else
				{
					pursuePlayer = true;
					pursueTimer.Start();
				}
				break;
		}
	}

	protected override void performAttack()
	{
		chooseAttack();
	}

	private void animationFinished()
	{
		switch (currentAnimation)
		{
			case CHARGE:
				player.takeDamage((int)(damage / 1.5f));
				isAttacking = false;
				attackCooldown.Start();
				break;
		}
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
		GD.Print("taking damage");
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
		CHARGE
	}
}