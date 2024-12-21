using Enums;
using Godot;
using System;
using static Enums.ItemType;
public partial class Item : Area2D
{
	[Signal]
	public delegate void onCoinCollectedEventHandler();

	private AnimatedSprite2D sprite;
	private ItemType type;
	private float initialDistance;
	private bool autoCollect = false;
	private Player player;
	private int speed = 50;

	public void init(Vector2 position, ItemType type)
	{
		Position = position;
		sprite.Animation = type.ToString().Capitalize();
		if (type == HEART)
		{
			sprite.Scale = new Vector2(.5f, .5f);
		}
		if (!IsPhysicsProcessing())
		{
			SetPhysicsProcess(true);
			Show();
		}
		sprite.Play();
		GD.Print("Generating " + type);
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("ItemSprite");
		player = GetParent().GetNode<Player>("Player");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (autoCollect)
		{
			var direction = player.GlobalPosition - GlobalPosition;
			var speed = Mathf.Lerp(150, 250, direction.Length() / initialDistance);
			GlobalPosition += direction.Normalized() * speed * (float)delta;
		}
	}

	private void onBodyDetected(Node2D body)
	{
		if (body is Player p)
		{
			autoCollect = false;
			switch (type)
			{
				case COIN:
					p.gold++;
					break;
				case HEART:
					p.health = Math.Min(p.maxHealth, p.health + 50);
					p.EmitSignal(nameof(p.onHealthChanged), p.health);
					break;
			}
			PoolEngine.instance.addToPool(this);
		}
	}

	public void resetItem()
	{
		Position = Vector2.Zero;
		Scale = Vector2.One;
		autoCollect = false;
		sprite.Stop();
	}

	private void autoCollectItem()
	{
		initialDistance = (player.GlobalPosition - GlobalPosition).Length();
		autoCollect = true;
	}
}