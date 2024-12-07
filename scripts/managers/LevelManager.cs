using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public partial class LevelManager : Node
{

	private Vector2I[] sandTiles = { new Vector2I(5, 5), new Vector2I(6, 5), new Vector2I(7, 5), new Vector2I(8, 5), new Vector2I(9, 5), new Vector2I(2, 32) };
	private Vector2I[] decorationTiles = { new Vector2I(40, 0), new Vector2I(41, 0) };
	private RandomNumberGenerator rnd;
	private List<TileMapLayer> layers;

	public LevelManager() { }

	public LevelManager(TileMapLayer baseLayer)
	{
		layers = new List<TileMapLayer>();
		rnd = new RandomNumberGenerator();
		layers.Add(baseLayer);
		baseLayer.GetChildren().OfType<TileMapLayer>().ToList().ForEach(x =>
		{
			layers.Add(x);
		});
	}

	public void generateLevel(int level)
	{
		generateTerrain();
	}

	private void generateTerrain()
	{
		var arenaCells = layers[0].GetUsedCellsById(0, sandTiles[5], 1);

		var cellPos = new Vector2I();
		var mapSize = layers[0].GetUsedRect();
		for (int i = 0; i < mapSize.Size.X; i++)
		{
			for (int y = 0; y < mapSize.Size.Y; y++)
			{
				var cellType = rnd.Randf() switch
				{
					< .85f => sandTiles[0],
					< .90f => sandTiles[1],
					< .93f => sandTiles[2],
					< .97f => sandTiles[3],
					_ => sandTiles[4],
				};
				cellPos.X = i;
				cellPos.Y = y;
				layers[0].SetCell(cellPos, 0, cellType);
			}
		}
		arenaCells.ToList().ForEach(x =>
		{
			layers[0].SetCell(x, 0, decorationTiles[0]);
		});
	}

}
