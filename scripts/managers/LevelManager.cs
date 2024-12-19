using Enums;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using static Enums.TileType;

public partial class LevelManager
{
	private readonly Vector2I TILE_SIZE;
	private GameController game;
	Timer trapCooldown;
	private Godot.Collections.Dictionary<TileType, Array<Vector2I>> tilesMap = new Godot.Collections.Dictionary<TileType, Array<Vector2I>>
	{
		{SAND, new Array<Vector2I> { new Vector2I(5, 5), new Vector2I(6, 5), new Vector2I(7, 5), new Vector2I(8, 5), new Vector2I(9, 5), new Vector2I(2, 32) }},
		{GRASS, new Array<Vector2I> { new Vector2I(5, 12), new Vector2I(6, 12), new Vector2I(7, 12), new Vector2I(8, 12), new Vector2I(9, 12) }},
		{SNOW, new Array<Vector2I> { new Vector2I(5, 19), new Vector2I(6, 19), new Vector2I(7, 19), new Vector2I(8, 19), new Vector2I(9, 19) }},
		{FOREST, new Array<Vector2I> { new Vector2I(16, 12), new Vector2I(17, 12), new Vector2I(18, 12), new Vector2I(19, 12), new Vector2I(20, 12) }},
		{TRAP, new Array<Vector2I> { new Vector2I(5, 1), new Vector2I(4, 1) }},
		{DECOR, new Array<Vector2I> { new Vector2I(27, 2), new Vector2I(28, 2), new Vector2I(29, 2), new Vector2I(30, 2), new Vector2I(27, 1), new Vector2I(32, 0), new Vector2I(40, 0), new Vector2I(41, 0)}},
		{DOOR, new Array<Vector2I> { new Vector2I(22, 9), new Vector2I(22, 6)}},
	};

	private RandomNumberGenerator rnd;
	private List<TileMapLayer> layers;
	private Array<Vector2I> arenaCells;
	private Rect2 arenaRect;

	private Godot.Collections.Dictionary<Vector2I, Rect2> trapCells;
	private Array<BiomeConfig> biomes;
	private int level;
	private int wave;
	private BiomeConfig biome;
	private Array<PackedScene> enemyScenes;
	private int enemiesKilled;
	private Area2D doorArea;
	private int spawnCount = 0;

	public LevelManager(GameController game, TileMapLayer baseLayer)
	{
		this.game = game;
		trapCooldown = new Timer();
		trapCooldown.WaitTime = 3f;
		trapCooldown.Timeout += onTrapTrigger;
		baseLayer.AddChild(trapCooldown);
		layers = new List<TileMapLayer>();
		trapCells = new Godot.Collections.Dictionary<Vector2I, Rect2>();
		rnd = new RandomNumberGenerator();
		this.biomes = game.biomeConfigs;
		this.level = game.level;
		doorArea = game.GetNode<Area2D>("Arena/DoorArea");

		//Initialize all Tiled Map Layers
		layers.Add(baseLayer);
		baseLayer.GetChildren().OfType<TileMapLayer>().ToList().ForEach(x =>
		{
			layers.Add(x);
		});
		arenaCells = layers[1].GetUsedCellsById(6, tilesMap[SAND][5], 1);
		TILE_SIZE = baseLayer.TileSet.TileSize;
		arenaRect = new Rect2(arenaCells[0], TILE_SIZE);
		foreach (var c in arenaCells)
		{
			arenaRect = arenaRect.Expand(c);
		}
		generateLevel();
		doorArea.BodyEntered += onDoorCrossed;
		SignalBus.bus.onEnemyKilled += enemyKilled;
	}

	public void generateLevel()
	{
		//TODO Balance trap numbers and spawns based on level/waves
		//TODO Biome obstacles?

		//Clear dead enemies and projectiles
		if (level >= 1)
		{
			game.GetTree().CallGroup("Enemies", "queue_free");
			game.GetTree().CallGroup("Projectiles", "queue_free");
			PoolEngine.instance.projectilePool.Clear();
			game.buffer.resetBuffer();
		}
		level++;
		//Show Level Notification
		SignalBus.bus.EmitSignal("onNotifyPlayer", $"LEVEL   {level}", Colors.White);
		wave = 0;
		//Select Biome
		switch (level)
		{
			case <= 5:
				biome = biomes[0];
				break;
			case <= 10:
				biome = biomes[1];
				break;
			case <= 15:
				biome = biomes[2];
				break;
			case <= 20:
				biome = biomes[3];
				break;
			default:
				biome = biomes[0];
				break;

		}
		//Update UI with new level
		SignalBus.bus.EmitSignal("onLevelCompleted", level, biome.iconPath);
		//Get biome enemies
		enemyScenes = new Array<PackedScene>();
		foreach (var e in biome.enemies)
		{
			var name = e.ToString().Capitalize();
			enemyScenes.Add(GD.Load<PackedScene>($"res://scenes/entities/{name}Enemy.tscn"));
		}
		generateTerrain();
		generateArenaCells();
		generateSpawns();
	}

	public void generateSpawns()
	{
		wave = 5;
		var maxDistance = 9;
		int minDistance = maxDistance - wave;
		spawnCount = 1;
		var enemyTypes = new Array<EnemyType>();
		switch (wave)
		{
			case 1:
				spawnCount++;
				minDistance++;
				enemyTypes.Add(biome.enemies[0]);
				break;
			case 2:
				enemyTypes.Add(biome.enemies[1]);
				break;
			case 3:
				enemyTypes = biome.enemies;
				break;
			case 4:
				enemyTypes = biome.enemies;
				break;
			case 5:
				enemyTypes = biome.enemies;
				break;
			default:
				spawnCount = 10;
				minDistance = 4;
				enemyTypes = biome.enemies;
				break;
		};
		SignalBus.bus.EmitSignal("onWaveCompleted", wave);
		var spawns = poissonDiskSampling(spawnCount, minDistance);
		int rndEnemy;
		foreach (var s in spawns)
		{
			if (wave == 1)
			{
				rndEnemy = 0;
			}
			else if (wave == 2)
			{
				rndEnemy = 1;
			}
			else
			{
				rndEnemy = rnd.RandiRange(0, enemyTypes.Count - 1);
			}
			var enemy = enemyScenes[rndEnemy].Instantiate<Enemy>();
			enemy.GlobalPosition = layers[0].MapToLocal(s);
			game.CallDeferred("add_child", enemy);
		}
		var timer = game.GetTree().CreateTimer(.75f);
		timer.Timeout += playSpawn;


	}

	private void playSpawn()
	{
		AssetManager.instance.playSFX("enemySpawn", -7.5f);
	}

	private void generateTerrain()
	{
		Vector2I cellPos;
		//Whole Map
		var mapSize = layers[0].GetUsedRect();
		for (int i = 0; i < mapSize.Size.X; i++)
		{
			for (int y = 0; y < mapSize.Size.Y; y++)
			{
				//Select Random Terrain Tiles
				var cellType = rnd.Randf() switch
				{
					< .85f => 0,
					< .89f => 1,
					< .93f => 2,
					< .97f => 3,
					_ => 4,
				};
				cellPos.X = i;
				cellPos.Y = y;
				//Generate base terrain in case its arena area
				if (arenaCells.Contains(cellPos))
				{
					layers[0].SetCell(cellPos, 0, tilesMap[biome.name][0]);
					layers[1].SetCell(cellPos, 6, tilesMap[biome.name][0]);
				}
				else
					layers[0].SetCell(cellPos, 0, tilesMap[biome.name][cellType]);
			}
		}
	}

	private void generateArenaCells()
	{
		trapCells.Clear();
		arenaCells.ToList().ForEach(x =>
		{
			Vector2I cellType;
			if (rnd.Randf() < .80f) return;
			var rand = rnd.Randf();
			switch (rand)
			{
				case < .90f:
					//Create decoration
					var isSkull = rnd.Randf() > .90f;
					if (isSkull)
						rand = rnd.RandiRange(tilesMap[DECOR].Count - 2, tilesMap[DECOR].Count - 1);
					else
						rand = rnd.RandiRange(0, tilesMap[DECOR].Count - 3);
					cellType = tilesMap[DECOR][(int)rand];
					layers[1].SetCell(x, 6, cellType);
					break;
				case < 1f:
					//Create traps
					if (level >= 3)
					{
						cellType = tilesMap[TRAP][0];
						layers[1].SetCell(x, 2, cellType);
						var mapPos = layers[0].MapToLocal(x);
						trapCells.Add(x, new Rect2(mapPos.X - TILE_SIZE.X / 2, mapPos.Y - TILE_SIZE.Y / 2, TILE_SIZE.X, TILE_SIZE.Y));
					}
					else
					{
						rand = rnd.RandiRange(0, tilesMap[DECOR].Count - 3);
						cellType = tilesMap[DECOR][(int)rand];
						layers[1].SetCell(x, 6, cellType);
					}
					break;
			}
		});
		game.trapsPosition.ToList().Clear();
		game.trapsPosition = (Array<Rect2>)trapCells.Values;
		trapCooldown.Start();
	}

	private async void onTrapTrigger()
	{
		//Activate traps
		trapCells.ToList().ForEach(x =>
				{
					layers[1].SetCell(x.Key, 2, tilesMap[TRAP][1]);
				});

		game.areTrapsActive = true;

		//Reset traps after short delay
		await game.ToSignal(game.GetTree().CreateTimer(.75f), "timeout");
		trapCells.ToList().ForEach(x =>
		{
			layers[1].SetCell(x.Key, 2, tilesMap[TRAP][0]);
		});
		game.areTrapsActive = false;
	}

	//DISTRIBUTING SPACE EVENLY
	private List<Vector2I> poissonDiskSampling(int numPoints, float minDistance, int attempts = 50)
	{
		List<Vector2I> points = new List<Vector2I>() { };
		List<Vector2I> spawnPoints = new List<Vector2I> { (Vector2I)arenaRect.GetCenter() };

		while (spawnPoints.Count > 0 && points.Count < numPoints)
		{
			int spawnIndex = rnd.RandiRange(0, spawnPoints.Count - 1);
			Vector2I spawnCenter = spawnPoints[spawnIndex];
			bool found = false;

			for (int i = 0; i < attempts; i++)
			{
				float angle = rnd.Randf() * Mathf.Tau;
				float radius = rnd.RandfRange(minDistance, minDistance * 2);
				Vector2I candidate = spawnCenter + new Vector2I((int)(Mathf.Cos(angle) * radius), (int)(Mathf.Sin(angle) * radius));

				if (arenaRect.HasPoint(candidate))
				{
					bool valid = true;
					foreach (var point in points)
					{
						if ((candidate - point).Length() < minDistance)
						{
							valid = false;
							break;
						}
					}

					if (valid)
					{
						points.Add(candidate);
						spawnPoints.Add(candidate);
						found = true;
						break;
					}
				}
			}

			if (!found)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}
		}
		return points;
	}

	//Open or close the Arena Door
	public void openLevelDoor(bool opening)
	{
		Array<Vector2I> door;
		if (opening)
		{
			door = layers[1].GetUsedCellsById(1, tilesMap[DOOR][0]);
			layers[1].SetCell(door[0], 1, tilesMap[DOOR][1]);
			AssetManager.instance.playSFX("levelCompleted");
			SignalBus.bus.EmitSignal("onNotifyPlayer", $"LEVEL COMPLETED", Colors.Green);
			AssetManager.instance.playSFX("openDoor");
			game.buffer.animation.Play("drop");
		}
		else
		{
			door = layers[1].GetUsedCellsById(1, tilesMap[DOOR][1]);
			layers[1].SetCell(door[0], 1, tilesMap[DOOR][0]);
		}
	}

	//Controls if Wave or Level have finished
	private void enemyKilled()
	{
		enemiesKilled++;
		if (enemiesKilled == spawnCount)
		{
			if (wave == 5)
			{
				openLevelDoor(true);
			}
			else
			{
				generateSpawns();
			}
			enemiesKilled = 0;
		}
	}

	//Next Level
	private void onDoorCrossed(Node2D body)
	{
		if (body is Player p)
		{
			AssetManager.instance.playSFX("doorTp");
			openLevelDoor(false);
			generateLevel();
			p.GlobalPosition = game.playerSpawn.Position;
		}
	}
}