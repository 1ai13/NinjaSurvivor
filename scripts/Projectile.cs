using Godot;
using System;

public partial class Projectile : Area2D
{
	[Export]
	private float speed = 200f;
	private float rotationSpeed = 500f;
	private bool isAlive = true;
	private int damage { get; set; }
	private const int offset = 20;
	private Vector2 initialPosition { get; set; }
	private float initialRotation { get; set; }
	private Vector2 velocity { get; set; } = Vector2.Zero;
	[Export]
	public bool isProjectile { get; set; }

	public void init(Vector2 position, Vector2 vel, float rotation, int dmg)
	{
		velocity = vel;
		damage = dmg;
		// Sets the bullet away from the player
		Position = position + velocity * offset;
		Rotation = rotation;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (isAlive)
		{
			Position += velocity * speed * (float)delta;
			if (!isProjectile)
			{
				Rotation += rotationSpeed * (float)delta;
			}
		}
	}

	private void onAreaDetected(Area2D area)
	{
		if (area.GetParent() is Enemy e)
		{
			e.takeDamage(damage);
			QueueFree();
		}
	}

	private void onBodyEntered(Node2D body)
	{
		isAlive = false;
	}
}
