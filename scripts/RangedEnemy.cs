using Godot;
using System;

public partial class RangedEnemy : Enemy
{
	private PackedScene projectileScene;
	private bool projectileDone;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		EntityHelper.initEnemy(this, data);
		base._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (attackCooldown.IsStopped() && projectileDone)
		{
			projectileDone = false;
		}
	}

	protected override void performAttack()
	{
		EntityHelper.playAnimation(this, "attack");
		isAttacking = true;
	}

	protected override void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack") && !projectileDone)
		{
			projectileDone = true;
			enemyDirection = distanceToPlayer.Normalized();
			var projectile = PoolEngine.pool.pullFromPool<Projectile>();
			projectile.init(GlobalPosition, enemyDirection, enemyDirection.Angle(), this, data.projectileSpeed, data.angularSpeed, data.isProjectile, type.ToString().Capitalize(), 0, 0);
		}
		base.onAnimationFinished(animationName);
	}
}
