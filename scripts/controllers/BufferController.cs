using Godot;
using Godot.Collections;
using System;

public partial class BufferController : Node2D
{
	[Export]
	private Array<Buff> buffs;
	public AnimationPlayer animation;
	public StaticBody2D bufferBody;
	private Vector2 initialPosition;
	private Vector2 nodeInitial;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		bufferBody = GetNode<StaticBody2D>("BufferBody");
		initialPosition = bufferBody.GlobalPosition;
		nodeInitial = Position;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void onPlayerNear(Node2D body)
	{
		//Generate a Random buff from Buffs Pool
		if (body is Player p)
		{
			bool removeBuff = false;
			var randomBuffs = new Array<Buff>();
			var currentBuffs = buffs.Duplicate();
			//Generating 3 random buffs
			for (int i = 0; i < 3; i++)
			{
				var rndBuff = currentBuffs.PickRandom();
				GD.Print("Inserting buff" + rndBuff.type);
				randomBuffs.Add(rndBuff);
				currentBuffs.Remove(rndBuff);
			}
			//Showing random buffs
			for (int i = 0; i < randomBuffs.Count; i++)
			{
				var buff = randomBuffs[i];
				GD.Print($"{i + 1} - {buff.type} -> {buff.description}");
			}
			//Applying selected buff
			GD.Print("Buff SELECTED " + randomBuffs[0].type);
			if (randomBuffs[0].isDirect)
			{
				removeBuff = randomBuffs[0].applyBuff(p);
			}
			else
			{
				p.buffPool.Add(randomBuffs[0]);
			}
			if (removeBuff)
			{
				buffs.Remove(randomBuffs[0]);
			}
		}
	}

	public void resetBufferPosition()
	{
		//Reset random position around door
		if (EntityHelper.rnd.Randf() <= .5f)
		{
			Position = nodeInitial;
		}
		else
		{
			Position = nodeInitial + Vector2.Right * 115;
		}
		bufferBody.GlobalPosition = initialPosition;
	}
}