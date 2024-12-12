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
	}

	protected override void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack"))
		{
			enemyDirection = distanceToPlayer.Normalized();
			var projectile = PoolEngine.instance.pullFromPool();
			projectile.init(GlobalPosition, enemyDirection, enemyDirection.Angle(), this, data.projectileSpeed, data.angularSpeed, data.isProjectile, type.ToString().Capitalize());
			projectile.projectileHitPlayer += onPlayerHit;
		}
		base.onAnimationFinished(animationName);
	}

	public void onPlayerHit()
	{
		player.takeDamage(damage);
	}
}
