using Godot;
using System;
using System.Buffers;

public partial class Projectile : Area2D
{
	[Signal]
	public delegate void projectileHitAreaEventHandler(Area2D area);
	[Signal]
	public delegate void projectileHitPlayerEventHandler(Player player);
	[Export]
	private float speed = 200f;
	private float rotationSpeed = 500f;
	private bool isAlive = true;
	private const int offset = 20;
	private Vector2 initialPosition { get; set; }
	private float initialRotation { get; set; }
	private Vector2 velocity { get; set; } = Vector2.Zero;
	[Export]
	public bool isProjectile { get; set; }
	private Node2D owner;

	public void init(Vector2 position, Vector2 vel, float rotation, Node2D owner)
	{
		this.owner = owner;
		velocity = vel;
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
			EmitSignal(SignalName.projectileHitArea, area);
			QueueFree();
		}
	}

	//On Wall or Player collision 
	private void onBodyEntered(Node2D body)
	{
		isAlive = false;
		if (owner is RangedEnemy && body is Player p)
		{
			EmitSignal(SignalName.projectileHitPlayer, (Player)body);
			QueueFree();
		}
		else if (owner is not Player)
		{
		}
		else
		{
			AssetManager.instance.playSFX("rangedWallHit", -5f);
			QueueFree();
		}

	}
}
