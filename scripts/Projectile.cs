using Enums;
using Godot;
using System;
using System.Buffers;
using static Enums.EnemyType;

public partial class Projectile : Area2D
{
	[Signal]
	public delegate void projectileHitAreaEventHandler(Area2D area);
	[Signal]
	public delegate void projectileHitPlayerEventHandler(Player player);
	[Export]
	public float speed = 200f;
	public float angularSpeed = 500f;
	private const int offset = 20;
	private Vector2 initialPosition { get; set; }
	private float initialRotation { get; set; }
	private Vector2 velocity { get; set; } = Vector2.Zero;
	[Export]
	public bool isProjectile { get; set; }
	private Node2D owner;
	public Texture2D sprite;

	public void init(Vector2 position, Vector2 vel, float rotation, Node2D owner, float s, float angularS, bool isProjectile, Texture2D sprite)
	{
		this.owner = owner;
		velocity = vel;
		// Sets the bullet away from the player
		Position = position + velocity * offset;
		Rotation = rotation;
		speed = s;
		angularSpeed = angularS;
		this.isProjectile = isProjectile;
		this.sprite = sprite;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Sprite2D>("ProjectileSprite").Texture = sprite;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (IsPhysicsProcessing())
		{
			Position += velocity * speed * (float)delta;
			if (!isProjectile)
			{
				Rotation += angularSpeed * (float)delta;
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
		if (owner is RangedEnemy o && body is Player p)
		{
			EmitSignal(SignalName.projectileHitPlayer, p);
			playHitSound(o.type);
			QueueFree();
		}
		else if (owner is RangedEnemy own)
		{
			playHitSound(own.type);
			QueueFree();
		}
		else
		{
			SetPhysicsProcess(false);
			AssetManager.instance.playSFX("rangedWallHit", -5f);
		}

	}

	private void playHitSound(EnemyType type)
	{
		switch (type)
		{
			case BAT:
				AssetManager.instance.playSFX("batAttackHit");
				break;
			default:
				GD.PrintErr("No wall hit sound available for projectile");
				break;
		}
	}

}
