using Godot;
using System;

public partial class SignalBus : Node
{

	public static SignalBus bus { get; private set; }


	//Init method
	public override void _Ready()
	{
		bus = this;
	}
}
