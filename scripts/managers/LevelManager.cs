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
	Timer trapCooldown;
	private Godot.Collections.Dictionary<TileType, Array<Vector2I>> tilesMap = new Godot.Collections.Dictionary<TileType, Array<Vector2I>>
	{
		{SAND, new Array<Vector2I> { new Vector2I(5, 5), new Vector2I(6, 5), new Vector2I(7, 5), new Vector2I(8, 5), new Vector2I(9, 5), new Vector2I(2, 32) }},
		{GRASS, new Array<Vector2I> { new Vector2I(5, 12), new Vector2I(6, 12), new Vector2I(7, 12), new Vector2I(8, 12), new Vector2I(9, 12) }},
		{SNOW, new Array<Vector2I> { new Vector2I(5, 19), new Vector2I(6, 19), new Vector2I(7, 19), new Vector2I(8, 19), new Vector2I(9, 19) }},
		{FOREST, new Array<Vector2I> { new Vector2I(16, 12), new Vector2I(17, 12), new Vector2I(18, 12), new Vector2I(19, 12), new Vector2I(20, 12) }},
		{TRAP, new Array<Vector2I> { new Vector2I(5, 1), new Vector2I(4, 1) }},
		{DECOR, new Array<Vector2I> { new Vector2I(27, 2), new Vector2I(28, 2), new Vector2I(29, 2), new Vector2I(30, 2), new Vector2I(27, 1), new Vector2I(32, 0), new Vector2I(40, 0), new Vector2I(41, 0)}},
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

	public LevelManager(TileMapLayer baseLayer, Array<BiomeConfig> biomes, int level)
	{
		trapCooldown = new Timer();
		trapCooldown.WaitTime = 3f;
		trapCooldown.Timeout += onTrapTrigger;
		baseLayer.AddChild(trapCooldown);
		layers = new List<TileMapLayer>();
		trapCells = new Godot.Collections.Dictionary<Vector2I, Rect2>();
		rnd = new RandomNumberGenerator();
		this.biomes = biomes;
		this.level = level;
		wave = 0;
		//Save all Tiled Map Layers
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
	}
	public void generateLevel()
	{
		biome = level switch
		{
			<= 5 => biomes[0],
			<= 10 => biomes[1],
			<= 15 => biomes[2],
			<= 20 => biomes[3],
			_ => biomes[0]
		};

		generateTerrain();
		generateArenaCells();
		generateSpawns();
	}

	private void generateSpawns()
	{
		wave++;
		var spawnCount = 0;
		var minDistance = 0;
		var enemyTypes = new Array<EnemyType>();
		switch (wave)
		{
			case 1:
				spawnCount = 3;
				minDistance = 10;
				enemyTypes.Add(biome.enemies[0]);
				break;
			case 2:
			case 3:
				spawnCount = 5;
				minDistance = 6;
				enemyTypes.Add(biome.enemies[1]);
				break;
			case 4:
				spawnCount = 7;
				minDistance = 4;
				enemyTypes = biome.enemies;
				break;
			case 5:
				spawnCount = 10;
				minDistance = 3;
				enemyTypes = biome.enemies;
				break;
		};
		var spawns = poissonDiskSampling(spawnCount, minDistance);
		var enemyScenes = new Array<PackedScene>();
		foreach (var e in enemyTypes)
		{
			var name = e.ToString().Capitalize();
			enemyScenes.Add(GD.Load<PackedScene>($"res://scenes/{name}Enemy.tscn"));
		}
		foreach (var s in spawns)
		{
			var rndEnemy = rnd.RandiRange(0, enemyTypes.Count - 1);
			GD.Print("world coords pos:" + layers[0].MapToLocal(s));
			var enemy = enemyScenes[rndEnemy].Instantiate<Enemy>();
			enemy.GlobalPosition = layers[0].MapToLocal(s);
			GD.Print("enemy pos:" + enemy.GlobalPosition);
			layers[0].GetParent().AddChild(enemy);
			layers[1].SetCell(s, 6, tilesMap[DECOR][tilesMap[DECOR].Count - 2]);
		}
	}

	private void generateTerrain()
	{
		var cellPos = new Vector2I();
		//Whole Map
		var mapSize = layers[0].GetUsedRect();
		for (int i = 0; i < mapSize.Size.X; i++)
		{
			for (int y = 0; y < mapSize.Size.Y; y++)
			{
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
				//Dont generate terrain in case its arena area
				if (arenaCells.Contains(cellPos))
					layers[0].SetCell(cellPos, 0, tilesMap[biome.name][0]);
				else
					layers[0].SetCell(cellPos, 0, tilesMap[biome.name][cellType]);
			}
		}
	}

	private void generateArenaCells()
	{
		arenaCells.ToList().ForEach(x =>
		{
			Vector2I cellType;
			if (rnd.Randf() < .75f) return;
			var rand = rnd.Randf();
			switch (rand)
			{
				case < .90f:
					//Create decoration
					rand = rnd.RandiRange(0, tilesMap[DECOR].Count - 3);
					cellType = tilesMap[DECOR][(int)rand];
					layers[1].SetCell(x, 6, cellType);
					break;
				case < 1f:
					//Create traps
					cellType = tilesMap[TRAP][0];
					layers[1].SetCell(x, 2, cellType);
					var mapPos = layers[0].MapToLocal(x);
					trapCells.Add(x, new Rect2(mapPos.X - TILE_SIZE.X / 2, mapPos.Y - TILE_SIZE.Y / 2, TILE_SIZE.X, TILE_SIZE.Y));
					break;
					// case < 1f:
					// 	//Create skulls
					// 	var cell = rnd.RandiRange(tilesMap[DECOR].Count - 2, tilesMap[DECOR].Count - 1);
					// 	cellType = tilesMap[DECOR][cell];
					// 	layers[1].SetCell(x, 6, cellType);
					// 	break;
			}
		});
		SignalBus.bus.EmitSignal("onTrapsCreated", (Array<Rect2>)trapCells.Values);
		trapCooldown.Start();
	}

	private async void onTrapTrigger()
	{
		//Activate traps
		trapCells.ToList().ForEach(x =>
				{
					layers[1].SetCell(x.Key, 2, tilesMap[TRAP][1]);
				});

		SignalBus.bus.EmitSignal("onTrapsActive");

		//Reset traps after short delay
		var node = layers[0];
		await node.ToSignal(node.GetTree().CreateTimer(.75f), "timeout");
		trapCells.ToList().ForEach(x =>
		{
			layers[1].SetCell(x.Key, 2, tilesMap[TRAP][0]);
		});
		SignalBus.bus.EmitSignal("onTrapsInactive");
	}

	//TODO DISTRIBUTING EVENLY INTO SPACE Study more this algorithm , working with some own adjustments
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
				float angle = rnd.Randf() * MathF.PI * 2f;
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
}