using Godot;
using System;

public partial class Game : Node2D
{
	private Player player;
	private Marker2D playerSpawn;
	[Export]
	private string characterSelected;
	[Export]
	private Character[] characters;

	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		playerSpawn = GetNode<Marker2D>("Arena/Marker2D");
		foreach (var c in characters)
		{
			if (c.name.Equals(characterSelected))
			{
				player.loadCharacter(c, playerSpawn.Position);
			}
		}
	}

	public override void _Process(double delta)
	{

	}
}
