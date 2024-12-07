using Enums;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using static Enums.TileType;

public partial class LevelManager : Node
{
	Timer trapController;
	private Godot.Collections.Dictionary<TileType, Array<Vector2I>> tilesMap = new Godot.Collections.Dictionary<TileType, Array<Vector2I>>
	{
		{SAND, new Array<Vector2I> { new Vector2I(5, 5), new Vector2I(6, 5), new Vector2I(7, 5), new Vector2I(8, 5), new Vector2I(9, 5), new Vector2I(2, 32) }},
		{GRASS, new Array<Vector2I> { new Vector2I(5, 12), new Vector2I(6, 12), new Vector2I(7, 12), new Vector2I(8, 12), new Vector2I(9, 12) }},
		{TRAP, new Array<Vector2I> { new Vector2I(5, 1), new Vector2I(4, 1) }},
		{DECOR, new Array<Vector2I> { new Vector2I(27, 2), new Vector2I(28, 2), new Vector2I(29, 2), new Vector2I(30, 2), new Vector2I(27, 1), new Vector2I(32, 0) }},
	};

	private RandomNumberGenerator rnd;
	private List<TileMapLayer> layers;
	private Array<Vector2I> arenaCells;
	private Array<Vector2I> trapCells;

	public LevelManager(TileMapLayer baseLayer)
	{
		trapController = new Timer();
		trapController.WaitTime = 3;
		trapController.Timeout += onTrapTrigger;
		baseLayer.AddChild(trapController);
		layers = new List<TileMapLayer>();
		trapCells = new Array<Vector2I>();
		rnd = new RandomNumberGenerator();

		layers.Add(baseLayer);
		baseLayer.GetChildren().OfType<TileMapLayer>().ToList().ForEach(x =>
		{
			layers.Add(x);
		});
		arenaCells = layers[1].GetUsedCellsById(6, tilesMap[SAND][5], 1);
	}

	public void generateLevel(int level)
	{
		generateTerrain();
		generateArenaCells();
	}

	private void generateTerrain()
	{
		var cellPos = new Vector2I();
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
				if (arenaCells.Contains(cellPos))
					layers[0].SetCell(cellPos, 0, tilesMap[GRASS][0]);
				else
					layers[0].SetCell(cellPos, 0, tilesMap[GRASS][cellType]);
			}
		}
	}

	private void generateArenaCells()
	{

		arenaCells.ToList().ForEach(x =>
		{
			Vector2I cellType;
			var rand = rnd.Randf();
			if (rand < .8f) return;
			switch (rand)
			{
				case < .97f:
					cellType = tilesMap[DECOR].PickRandom();
					layers[1].SetCell(x, 6, cellType);
					break;
				case < 1f:
					GD.Print("printing trap");
					cellType = tilesMap[TRAP][0];
					layers[1].SetCell(x, 2, cellType);
					trapCells.Add(x);
					break;
				default:
					break;
			}
		});
		trapController.Start();
	}

	private void onTrapTrigger()
	{
		trapCells.ToList().ForEach(x =>
		{
			layers[1].SetCell(x, 2, tilesMap[TRAP][1]);
		});
	}
}