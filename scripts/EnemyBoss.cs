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
	private bool pursuePlayer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetParent().GetNode<Player>("Player");
		bossAnimation = GetNode<AnimatedSprite2D>("EnemyArea/BossSprite");
		attackCooldown = GetNode<Timer>("AttackCooldown");
		attackTypes = new Array<AttackType>() { MELEE };
		EntityHelper.initEnemy(this, data);
		setAnimation(IDLE);
		SignalBus.bus.onAwakeBoss += awakeBoss;
		SetPhysicsProcess(false);
	}

	public void init(Vector2 pos)
	{
		Position = pos;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		distanceToPlayer = player.GlobalPosition - GlobalPosition;
		//Out of range
		if (distanceToPlayer.Length() > attackRange && !isAttacking || pursuePlayer)
		{
			enemyDirection = distanceToPlayer.Normalized();
			if (pursuePlayer && distanceToPlayer.Length() < 20f)
			{
				pursuePlayer = false;
				setAnimation(CHARGE);
				isAttacking = true;
				return;
			}
			Position += enemyDirection * speed * (float)delta;
			setAnimation(WALK);
		}
		//In range
		else if (distanceToPlayer.Length() <= attackRange && attackCooldown.IsStopped() && !isAttacking)
		{
			chooseAttack(delta);
			randomDirection = false;
		}//Moving in random direction
		else if (distanceToPlayer.Length() <= attackRange && !attackCooldown.IsStopped() && !isAttacking)
		{
			if (!randomDirection)
			{
				GD.Print("walk random");
				GD.Print("attacl  cd " + !attackCooldown.IsStopped());
				enemyDirection = EntityHelper.getRandomDirection(enemyDirection);
				randomDirection = true;
			}
			Position += enemyDirection * speed * (float)delta;
			setAnimation(WALK);
		}
	}

	private void chooseAttack(double delta)
	{
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
				}
				break;
		}
	}

	protected override void performAttack()
	{
		throw new NotImplementedException();
	}

	private void animationFinished()
	{

		switch (currentAnimation)
		{
			case CHARGE:
				GD.Print("Finishing charge");
				player.takeDamage(damage / 2);
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
		player.setPlayerProcess(true);
		SetPhysicsProcess(true);
	}

	private void setAnimation(AnimationType type)
	{
		bossAnimation.Play(type.ToString().ToLower());
		currentAnimation = type;
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