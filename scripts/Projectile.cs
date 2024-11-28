using Godot;
using System;

public partial class Projectile : Area2D
{
	[Export]
	private float speed = 200f;
	private float rotationSpeed = 500f;
	private bool isAlive = true;
	private const int offset = 25;
	public Vector2 initialPosition { get; set; }
	public float initialRotation { get; set; }
	public Vector2 velocity { get; set; } = Vector2.Zero;
	[Export]
	public bool isProjectile { get; set; }

	public void init(Vector2 position, Vector2 velocity, float rotation)
	{
		initialPosition = position + velocity * offset;
		this.velocity = velocity;
		initialRotation = rotation;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Sets the bullet away from the player
		Position = initialPosition;
		Rotation = initialRotation;
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

	private void OnCollisionDetected(Node2D body)
	{
		isAlive = false;
	}
}
