using Godot;
using System;

public partial class Enemy : Node2D
{
	private const float speed = 100f;
	[Export]
	private int health { get; set; } = 100;
	[Export]
	private int damage = 10;
	private Player player;
	public Vector2 enemyDirection { get; set; }
	public AnimationPlayer animation;
	private bool isHit = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetParent().GetNode<Player>("Player");
		animation = GetNode<AnimationPlayer>("AnimationPlayer");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!isHit)
		{
			enemyDirection = (player.Position - Position).Normalized();
			Position += enemyDirection * speed * (float)delta;
			EntityHelper.playAnimation(this, "walk");
		}
	}

	private void onAnimationFinished(StringName animationName)
	{
		if (animationName.Equals("hit"))
		{
			isHit = false;
		}
	}

	private void onCollisionDetected(Node2D body)
	{
		if (body is Player p)
		{
			GD.Print("PLayer TAking damage");
			p.takeDamage(damage);
		}
	}

	public void takeDamage(int damage)
	{
		health -= damage;
		isHit = true;
		animation.Play("hit");
	}
}
