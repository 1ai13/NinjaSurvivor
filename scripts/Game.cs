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
	private int level = 0;
	private LevelManager levelManager;
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		var arena = GetNode<TileMapLayer>("Arena");
		levelManager = new LevelManager(arena);
		playerSpawn = GetNode<Marker2D>("Arena/Marker2D");
		foreach (var c in characters)
		{
			if (c.name.Equals(characterSelected))
			{
				player.loadCharacter(c, playerSpawn.Position);
			}
		}
		levelManager.generateLevel(level);
		//TODO Enemy spawner + MORE enemies
		//TODO Add more Levels / procedural logic level
	}

	public override void _Process(double delta)
	{

	}
}
