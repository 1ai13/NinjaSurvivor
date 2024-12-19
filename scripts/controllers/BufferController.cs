using Godot;
using Godot.Collections;
using System;

public partial class BufferController : Node2D
{
	[Signal]
	public delegate void onRandomBuffsGeneratedEventHandler(Array<Buff> randomBuffs);
	[Export]
	public Array<Buff> buffs;
	public AnimationPlayer animation;
	public StaticBody2D bufferBody;
	public AnimatedSprite2D bufferBubble;
	private Vector2 initialPosition;
	private Vector2 bufferInitialPos;
	public bool areBuffsGenerated;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		bufferBody = GetNode<StaticBody2D>("BufferBody");
		bufferBubble = GetNode<AnimatedSprite2D>("BufferBody/Bubble");
		initialPosition = bufferBody.GlobalPosition;
		bufferInitialPos = Position;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void onPlayerNear(Node2D body)
	{
		if (areBuffsGenerated) return;

		//Generate a Random buff from Buffs Pool
		if (body is Player p)
		{
			p.animation.CallDeferred("stop");
			p.SetPhysicsProcess(false);
			p.SetProcessInput(false);
			var randomBuffs = new Array<Buff>();
			var currentBuffs = buffs.Duplicate();
			var buffMinSize = 3;
			if (buffs.Count < buffMinSize)
			{
				buffMinSize = buffs.Count;
			}
			//Generating 3 random buffs
			for (int i = 0; i < buffMinSize; i++)
			{
				var rndBuff = currentBuffs.PickRandom();
				GD.Print("Inserting buff" + rndBuff.type);
				randomBuffs.Add(rndBuff);
				currentBuffs.Remove(rndBuff);
			}
			EmitSignal(SignalName.onRandomBuffsGenerated, randomBuffs);
			areBuffsGenerated = true;
		}
	}

	public void resetBuffer()
	{
		//Reset random position around door
		if (EntityHelper.rnd.Randf() <= .5f)
		{
			Position = bufferInitialPos;
		}
		else
		{
			Position = bufferInitialPos + Vector2.Right * 115;
		}
		bufferBody.GlobalPosition = initialPosition;
		areBuffsGenerated = false;
		bufferBubble.Play("dots");
	}
}