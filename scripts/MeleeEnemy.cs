using Godot;
using System;
using static Enums.EnemyType;

public partial class MeleeEnemy : Enemy
{
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
		player.takeDamage(damage);
		isAttacking = true;
		playAttackSound();
	}
}
