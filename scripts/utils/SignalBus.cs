using Godot;
using Godot.Collections;
using System;

public partial class SignalBus : Node
{

	public static SignalBus bus { get; private set; }

	[Signal]
	public delegate void onTrapsCreatedEventHandler(Array<Rect2> traps);
	[Signal]
	public delegate void onTrapsActiveEventHandler();
	[Signal]
	public delegate void onTrapsInactiveEventHandler();
	[Signal]
	public delegate void onEnemyKilledEventHandler();

	//Init method
	public override void _Ready()
	{
		bus = this;
	}
}
