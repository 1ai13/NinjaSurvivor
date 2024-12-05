using Godot;
using System;

public partial class RangedEnemy : Enemy
{
	private PackedScene projectileScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		projectileScene = AssetManager.instance.projectileScene;
		projectileScene = EntityHelper.packProjectileScene(projectileScene, data.enemySprites[1], data.isProjectile);
		health = data.health;
		base._Ready();
		EntityHelper.initEnemy(this, data);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}

	public override void performAttack()
	{
		EntityHelper.playAnimation(this, "attack");
		isAttacking = true;
		attackCooldown.Start();
	}

	private void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack"))
		{
			var projectile = projectileScene.Instantiate<Projectile>();
			projectile.init(GlobalPosition, enemyDirection, enemyDirection.Angle(), this);
			// projectile.pr += onPlayerHit;
			GetTree().CurrentScene.AddChild(projectile);
			isAttacking = false;
			AssetManager.instance.playSFX("rangedAttack");
		}
	}

	private void onPlayerHit(Player body)
	{
		GD.Print("Hitting player");
		player.takeDamage(damage);
	}
}
