using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class GameController : Node2D
{
	private Player player;
	private Marker2D playerSpawn;
	[Export]
	private string characterSelected;
	[Export]
	private Character[] characters;
	private int level = 0;
	private LevelManager levelManager;
	[Export]
	private Array<BiomeConfig> biomeConfigs;
	private Array<Rect2> trapsPosition;
	Vector2I playerFeetOffset = new Vector2I(0, 7);
	private bool areTrapsActive = false;
	private int enemiesKilled;

	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		var arena = GetNode<TileMapLayer>("Arena");
		playerSpawn = GetNode<Marker2D>("Arena/Marker2D");
		foreach (var c in characters)
		{
			if (c.name.Equals(characterSelected))
			{
				player.loadCharacter(c, playerSpawn.Position);
			}
		}
		trapsPosition = new Array<Rect2>();
		SignalBus.bus.onTrapsCreated += assignTraps;
		SignalBus.bus.onTrapsActive += onTrapsActive;
		SignalBus.bus.onTrapsInactive += onTrapsInactive;
		SignalBus.bus.onEnemyKilled += onEnemyKilled;
		levelManager = new LevelManager(arena, biomeConfigs, level);
		//TODO Add more Levels / procedural logic level
		//TODO MORE enemies
	}

	public override void _Process(double delta)
	{
		if (areTrapsActive)
		{
			foreach (var t in trapsPosition)
			{
				if (t.HasPoint(player.GlobalPosition + playerFeetOffset))
				{
					areTrapsActive = false;
					player.takeDamage((int)(player.maxHealth * 0.25f));
				}
			}
		}
	}

	private void onTrapsActive()
	{
		areTrapsActive = true;
	}

	private void onTrapsInactive()
	{
		areTrapsActive = false;
	}
	private void assignTraps(Array<Rect2> traps)
	{
		trapsPosition.ToList().Clear();
		trapsPosition = traps;
	}
	private void onEnemyKilled()
	{
		enemiesKilled++;
		if (enemiesKilled == levelManager.spawnCount)
		{
			if (levelManager.wave == 5)
			{
				levelManager.generateLevel();
			}
			else
			{
				levelManager.generateSpawns();
			}
			enemiesKilled = 0;
		}
	}
}
