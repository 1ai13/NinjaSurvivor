using Enums;
using Godot;
using System;

[GlobalClass]
public partial class EnemyData : Resource
{
	[Export]
	public EnemyType name { get; set; }
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
	public bool isProjectile;
	[Export]
	public float projectileSpeed;
	[Export]
	public float angularSpeed;
}