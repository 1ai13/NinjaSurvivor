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
	internal static int level;
	private int wave;
	private BiomeConfig biome;
	private int currentBiome;
	private Array<PackedScene> enemyScenes;
	private int enemiesKilled;
	private Area2D doorArea;
	private int spawnCount = 0;
	private int maxWaves;

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
		currentBiome = 0;
		biome = biomes[currentBiome];
		level = game.level;
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
		SignalBus.bus.onGenerateLevel += generateLevel;
		SignalBus.bus.onSpawnEnemies += generateSpawns;
		//Update UI with new level
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onWaveCompleted), 1, maxWaves);
	}

	public void generateLevel()
	{
		//TODO Balance trap numbers and spawns based on level/waves
		//TODO Biome obstacles?
		//Clear dead and unused entities and objects
		if (spawnCount != 0)
		{
			game.GetTree().CallGroup("Enemies", "queue_free");
			game.GetTree().CallGroup("Projectiles", "queue_free");
			game.GetTree().CallGroup("Items", "queue_free");
			PoolEngine.pool.pools[nameof(Projectile)].Clear();
			PoolEngine.pool.pools[nameof(Item)].Clear();
			game.buffer.animation.Stop();
			game.buffer.resetBuffer();
		}
		level++;
		maxWaves = 4 + level;
		//TODO REMOVE
		wave = maxWaves;
		if (level == 6)
		{
			swapArenas(true);
		}
		else
		{
			//Get biome enemies
			enemyScenes = new Array<PackedScene>();
			foreach (var e in biome.enemies)
			{
				var name = e.ToString().Capitalize();
				enemyScenes.Add(GD.Load<PackedScene>($"res://scenes/entities/{name}Enemy.tscn"));
			}
		}
		if (level == 0)
		{
			swapArenas(false);
		}
		generateTerrain();
		generateArenaCells();
		//Show Level Notification
		if (level > 0)
		{
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onNotifyPlayer), $"LEVEL   {level}", Colors.White);
			//Update UI with new level
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onLevelCompleted), level, biome.iconPath);
		}
		else
		{//Boss notification
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onNotifyPlayer), $"BOSS   STAGE", Colors.White);
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onLevelCompleted), level, biome.iconPath);
		}

	}

	private void swapArenas(bool arenaBoss)
	{
		if (arenaBoss)
		{
			var boss = biome.bossScene.Instantiate<EnemyBoss>();
			game.CallDeferred("add_child", boss);
			boss.init(game.bossSpawn.Position);

			level = -1;
			spawnCount = 1;
			layers[1].CollisionEnabled = false;
			layers[1].Visible = false;
			layers[2].CollisionEnabled = false;
			layers[2].Visible = false;
			layers[3].CollisionEnabled = true;
			layers[3].Visible = true;
			layers[4].CollisionEnabled = true;
			layers[4].Visible = true;
		}
		else
		{
			level++;
			layers[1].CollisionEnabled = true;
			layers[1].Visible = true;
			layers[2].CollisionEnabled = true;
			layers[2].Visible = true;
			layers[3].CollisionEnabled = false;
			layers[3].Visible = false;
			layers[4].CollisionEnabled = false;
			layers[4].Visible = false;
		}
	}

	public void generateSpawns()
	{
		wave++;
		var maxDistance = 9;
		int minDistance = maxDistance - wave;
		spawnCount = 1;
		// spawnCount = 1 * wave + 1;
		// spawnCount = 2 * wave + level;
		var enemyTypes = new Array<EnemyType>();
		switch (wave)
		{
			case 1:
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
				spawnCount = 1;
				minDistance = 4;
				enemyTypes = biome.enemies;
				break;
		};
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onWaveCompleted), wave, maxWaves);
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
				if (arenaCells.Contains(cellPos) && level != -1)
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
				case < .80f:
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
		int layerIndex;
		int sourceId;
		string message;
		if (level == -1)
		{
			message = "BIOME  COMPLETED";
			layerIndex = 3;
			sourceId = 2;
		}
		else
		{
			message = "LEVEL  COMPLETED";
			layerIndex = 1;
			sourceId = layerIndex;
		}
		if (opening)
		{
			if (level == -1)
			{
				currentBiome++;
				biome = biomes[currentBiome];
			}
			door = layers[layerIndex].GetUsedCellsById(sourceId, tilesMap[DOOR][0]);
			layers[layerIndex].SetCell(door[0], sourceId, tilesMap[DOOR][1]);
			AssetManager.instance.playSFX("levelCompleted");
			AssetManager.instance.playSFX("openDoor");
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onNotifyPlayer), message, Colors.Green);
			game.buffer.animation.Play("drop");
			if (level != -1)
			{
				SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onAutoCollectItem));
			}
		}
		else
		{
			AssetManager.instance.playSFX("openDoor");
			door = layers[layerIndex].GetUsedCellsById(sourceId, tilesMap[DOOR][1]);
			layers[layerIndex].SetCell(door[0], sourceId, tilesMap[DOOR][0]);
		}
	}

	//Controls if Wave or Level have finished
	private void enemyKilled()
	{
		enemiesKilled++;
		if (enemiesKilled == spawnCount)
		{
			if (wave == maxWaves)
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
			openLevelDoor(false);
			if (level == 5)
			{
				p.newLevelAnimation(true);
				return;
			}
			p.newLevelAnimation(false);
		}
	}
}