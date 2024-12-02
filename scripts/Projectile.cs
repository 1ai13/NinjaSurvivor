using Godot;
using System;
using System.Buffers;

public partial class Projectile : Area2D
{
	[Signal]
	public delegate void projectileHitEventHandler(Area2D area);
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
	private Node2D owner;

	public void init(Vector2 position, Vector2 vel, float rotation, int dmg, Node2D owner)
	{
		this.owner = owner;
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

	//Hitting enemy
	private void onAreaDetected(Area2D area)
	{
		if (owner is Player && area.GetParent() is Enemy e)
		{
			var dmg = damage;
			EmitSignal(SignalName.projectileHit, area);
			var isCrit = EntityHelper.getVariableDamage(dmg);
			QueueFree();
		}
	}

	//On Wall collision
	private void onBodyEntered(Node2D body)
	{
		isAlive = false;
		AssetManager.instance.playSFX("rangedWallHit", -5f);
	}
}
