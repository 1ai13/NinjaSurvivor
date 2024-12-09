using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Reflection;

public partial class PoolEngine : Node
{
	public static PoolEngine instance { get; private set; }
	private PackedScene projectileScene;
	public Array<Projectile> projectilePool;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		instance = this;
		projectileScene = AssetManager.instance.projectileScene;
		projectilePool = new Array<Projectile>();
	}

	public void addToPool(Projectile p)
	{
		p.SetPhysicsProcess(false);
		p.Hide();
		projectilePool.Add(p);
	}

	public Projectile pullFromPool()
	{

		if (projectilePool.Count == 0)
		{
			var projectile = projectileScene.Instantiate<Projectile>();
			GetTree().CurrentScene.AddChild(projectile);
			return projectile;
		}
		else
		{
			var projectile = projectilePool.Last();
			projectilePool.RemoveAt(projectilePool.IndexOf(projectilePool.Last()));
			return projectile;
		}
	}
}
