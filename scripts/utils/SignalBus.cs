using Enums;
using Godot;
using Godot.Collections;
using System;

public partial class SignalBus : Node
{

	public static SignalBus bus { get; private set; }

	[Signal]
	public delegate void onEnemyKilledEventHandler();
	[Signal]
	public delegate void onNotifyPlayerEventHandler(string message, Color color);
	[Signal]
	public delegate void onPlayerHealthBarUpdateEventHandler(int health);
	[Signal]
	public delegate void onLevelCompletedEventHandler(int level, string icon);
	[Signal]
	public delegate void onWaveCompletedEventHandler(int level);

	//Init method
	public override void _Ready()
	{
		bus = this;
	}
}
