using Godot;
using System;

public partial class Enemy : Node2D
{
	private const float speed = 90f;
	private const float attackRange = 18f;
	[Export]
	private int health { get; set; } = 100;
	[Export]
	private int damage = 10;
	private Player player;
	public Vector2 enemyDirection { get; set; }
	public AnimationPlayer animation;
	private Sprite2D enemySprite;
	private bool canMove = true;
	private bool isAttacking = false;
	private Timer attackCooldown;
	private Timer hitCooldown;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetParent().GetNode<Player>("Player");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		enemySprite = GetNode<Sprite2D>("EnemyArea/EnemySprite");
		attackCooldown = GetNode<Timer>("AttackCooldown");
		hitCooldown = GetNode<Timer>("HitCooldown");
		attackCooldown.Timeout += onAttackCooldownTimeout;
		hitCooldown.Timeout += onHitCooldownTimeout;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var distanceToPlayer = player.Position - Position;
		enemyDirection = distanceToPlayer.Normalized();

		if (distanceToPlayer.Length() > attackRange && canMove)
		{
			Position += enemyDirection * speed * (float)delta;
		}
		else if (distanceToPlayer.Length() <= attackRange && !isAttacking)
		{
			player.takeDamage(damage);
			isAttacking = true;
			attackCooldown.Start();
		}
		EntityHelper.playAnimation(this, "walk");
	}

	public void takeDamage(int damage)
	{
		health -= damage;
		var tween = CreateTween();
		tween.TweenProperty(enemySprite, "self_modulate", new Color(4, 4, 4, 4), .2f);
		tween.TweenProperty(enemySprite, "self_modulate", Colors.White, 0f);
		canMove = false;
		animation.Pause();
		hitCooldown.Start();
		AssetManager.instance.playSFX("enemyHit", -10f);
	}

	private void onAttackCooldownTimeout()
	{
		isAttacking = false;
	}

	private void onHitCooldownTimeout()
	{
		canMove = true;
	}
}
