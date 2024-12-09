using Godot;
using System;

public partial class RangedEnemy : Enemy
{
	private PackedScene projectileScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		health = data.health;
		base._Ready();
		EntityHelper.initEnemy(this, data);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}

	protected override void performAttack()
	{
		EntityHelper.playAnimation(this, "attack");
		isAttacking = true;
		attackCooldown.Start();
	}

	private void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack"))
		{
			var projectile = PoolEngine.instance.pullFromPool();
			projectile.init(GlobalPosition, enemyDirection, enemyDirection.Angle(), this, data.projectileSpeed, data.angularSpeed, data.isProjectile, data.enemySprites[2]);
			projectile.projectileHitPlayer += onPlayerHit;
			isAttacking = false;
			playAttackSound();
		}
	}

	public void onPlayerHit(Player body)
	{
		player.takeDamage(damage);
	}
}
