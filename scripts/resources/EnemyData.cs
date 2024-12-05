using Godot;
using System;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export]
	public string name { get; set; }
	[Export]
	public Texture2D[] enemySprites;
	[Export]
	public int health;
	[Export]
	public int damage;
	[Export]
	public float speed;
	[Export]
	public float range;
	[Export]
	public AudioStream deadSound;
	[Export]
	public bool isProjectile;
}