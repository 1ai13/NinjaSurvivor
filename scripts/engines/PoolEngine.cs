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
		//Resetting projectile
		p.SetPhysicsProcess(false);
		p.Hide();
		resetProjectile(p);
		projectilePool.Add(p);
	}

	public Projectile pullFromPool()
	{
		//Pulling or creating new Projectile
		if (projectilePool.Count == 0)
		{
			var projectile = projectileScene.Instantiate<Projectile>();
			GetTree().CurrentScene.AddChild(projectile);
			return projectile;
		}
		else
		{
			var projectile = projectilePool.Last();
			projectilePool.RemoveAt(projectilePool.Count - 1);
			return projectile;
		}
	}
	private void resetProjectile(Projectile p)
	{
		p.owner = null;
		p.velocity = Vector2.Zero;
		p.Position = Vector2.Zero;
		p.Rotation = 0;
		p.speed = 0;
		p.angularSpeed = 0;
		p.sprite = null;
	}
}
