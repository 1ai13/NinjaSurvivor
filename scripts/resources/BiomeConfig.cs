using Enums;
using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class BiomeConfig : Resource
{
	[Export]
	public Array<EnemyType> enemies;
	[Export]
	public TileType name;
	[Export]
	public string iconPath;
}