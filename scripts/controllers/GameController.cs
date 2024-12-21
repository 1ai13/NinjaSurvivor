using Godot;
using Godot.Collections;
using System;


public partial class GameController : Node2D
{
	private Player player;
	public Marker2D playerSpawn;
	[Export]
	private string characterSelected;
	[Export]
	private Character[] characters;
	public int level = 0;
	[Export]
	public Array<BiomeConfig> biomeConfigs;
	public Array<Rect2> trapsPosition;
	public BufferController buffer;
	Vector2I playerFeetOffset = new Vector2I(0, 7);
	public bool areTrapsActive = false;

	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		var arena = GetNode<TileMapLayer>("Arena");
		playerSpawn = GetNode<Marker2D>("Arena/PlayerRespawn");
		buffer = GetNode<BufferController>("Buffer");
		//Load selected Character
		foreach (var c in characters)
		{
			if (c.name.Equals(characterSelected))
			{
				player.loadCharacter(c, playerSpawn.Position);
			}
		}
		trapsPosition = new Array<Rect2>();
		new LevelManager(this, arena);
		//TODO Add more biomes & enemies
		//TODO Add BOSS fight
		//TODO Add game menu & character selection
	}

	public override void _Process(double delta)
	{
		//Hurt player in case standing on Traps
		if (areTrapsActive)
		{
			foreach (var t in trapsPosition)
			{
				if (t.HasPoint(player.GlobalPosition + playerFeetOffset))
				{
					areTrapsActive = false;
					player.takeDamage((int)(player.maxHealth * .25f));
				}
			}
		}
	}

}
