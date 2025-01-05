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
	private Player player;
	private int speed = 50;
	private Node2D owner;
	private bool isCollected;

	public void init(Vector2 position, ItemType type, Node2D owner)
	{
		this.owner = owner;
		Position = owner.Position;
		sprite.Animation = type.ToString().Capitalize();
		initialDistance = -1;
		this.type = type;
		isCollected = false;
		if (type == HEART)
		{
			sprite.Scale = Vector2.One / 2;
		}
		else
		{
			sprite.Scale = Vector2.One;
		}
		if (!IsPhysicsProcessing())
		{
			SetPhysicsProcess(true);
			Show();
		}
		sprite.Play();
		var tween = CreateTween();
		tween.TweenProperty(this, "position", position, .3f);
		if (type == PLANT_SCROLL)
		{
			Rotation = Mathf.Pi / 2;
			var t = CreateTween().SetLoops();
			t.TweenProperty(this, "skew", Mathf.Pi / 2, 1);
			t.TweenProperty(this, "skew", -Mathf.Pi / 2, 0);
			t.TweenProperty(this, "skew", 0, 1);
			t.TweenInterval(0);
		}
		AssetManager.instance.playSFX("itemDrop");
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
		if (player.autoCollect)
		{
			if (initialDistance == -1)
			{
				initialDistance = (player.GlobalPosition - GlobalPosition).Length();
			}
			var direction = player.GlobalPosition - GlobalPosition;
			var speed = Mathf.Lerp(150, 250, direction.Length() / initialDistance);
			GlobalPosition += direction.Normalized() * speed * (float)delta;
		}
	}

	private void onBodyDetected(Node2D body)
	{
		if (isCollected) return;
		if (body is Player p)
		{
			isCollected = true;
			switch (type)
			{
				case COIN:
					p.gold++;
					SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onCoinCollected));
					AssetManager.instance.playSFX("coinCollected");
					break;
				case HEART:
					p.health = Math.Min(p.maxHealth, p.health + 50);
					p.EmitSignal(nameof(p.onHealthChanged), 50);
					AssetManager.instance.playSFX("heartHeal", -5f);
					break;
				case PLANT_SCROLL:
					SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onScrollCollected));
					AssetManager.instance.playSFX(GD.Load<AudioStream>("res://assets/audio/items/scrollCollected.wav"));
					break;
			}
			PoolEngine.pool.addToPool(this);
		}
	}

	public void resetItem()
	{
		Position = Vector2.Zero;
		Scale = Vector2.One;
		initialDistance = -1;
		type = 0;
		sprite.Stop();
		Rotation = 0;
	}
}