using Godot;
using Godot.Collections;
using System;
using System.Linq;
public partial class PoolEngine : Node
{
	public static PoolEngine pool { get; private set; }
	public Dictionary<string, Array<Area2D>> pools;
	public Dictionary<string, PackedScene> scenes;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		pool = this;
		// Initialize pools and scenes
		pools = new Dictionary<string, Array<Area2D>>
	{
		{ nameof(Projectile), new Array<Area2D>() },
		{ nameof(Item), new Array<Area2D>() }
	};

		scenes = new Dictionary<string, PackedScene>
	{
		{ nameof(Projectile), AssetManager.instance.projectileScene },
		{ nameof(Item), AssetManager.instance.itemScene }
	};
	}

	public void addToPool(Area2D obj)
	{
		var key = obj.GetType().Name;
		obj.SetPhysicsProcess(false);
		obj.Hide();
		if (obj is Projectile p)
		{
			p.resetProjectile();
		}
		else if (obj is Item i)
		{
			i.resetItem();
		}
		pools[key].Add(obj);
	}

	public T pullFromPool<T>() where T : Area2D
	{
		var key = typeof(T).Name;
		if (pools[key].Count == 0)
		{
			var obj = scenes[key].Instantiate<T>();
			GetTree().CurrentScene.AddChild(obj);
			return obj;
		}
		else
		{
			try
			{
				var obj = (T)pools[key].Last();
				pools[key].Remove(obj);
				return obj;
			}
			catch (InvalidCastException)
			{
				var obj = scenes[key].Instantiate<T>();
				GetTree().CurrentScene.AddChild(obj);
				addToPool(obj);
				return obj;
			}

		}
	}
}
