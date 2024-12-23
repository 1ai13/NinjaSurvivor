using Enums;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using static Enums.WeaponType;
using static Enums.BuffType;
public partial class Player : CharacterBody2D
{
	[Signal]
	public delegate void onHealthChangedEventHandler(int value);
	public float speed;
	public int health { get; set; }
	public int maxHealth;
	public AnimationPlayer animation;
	private PackedScene projectileScene;
	private Area2D meleeWeapon;
	private Sprite2D rangedWeapon { get; set; }
	private bool isAttacking = false;
	public Vector2 mouseDirection { get; set; }
	public Vector2 damage { get; set; }
	private Sprite2D playerSprite;
	public Timer attackCooldown;
	private HashSet<Enemy> enemiesMeleeTargeted;
	private WeaponType currentType = MELEE;
	private Character characterData;
	public Camera2D camera;
	public Godot.Collections.Dictionary<BuffType, int> buffPool;
	public int gold;
	public float criticalChance;
	public float criticalDamage;
	public float dropLuck;
	public bool autoCollect;
	private Vector2 playerSpawn;

	public override void _Ready()
	{
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		meleeWeapon = GetNode<Area2D>("Area2D");
		rangedWeapon = GetNode<Sprite2D>("RangeWeapon");
		playerSprite = GetNode<Sprite2D>("Body");
		enemiesMeleeTargeted = new HashSet<Enemy>();
		attackCooldown = GetNode<Timer>("AttackCooldown");
		attackCooldown.Timeout += onAttackCooldownTimeout;
		SignalBus.bus.onAutoCollectItem += autoCollectMode;
		camera = GetNode<Camera2D>("Camera2D");
		buffPool = new Godot.Collections.Dictionary<BuffType, int>();
		gold = 0;
		criticalChance = .05f;
		criticalDamage = 1.5f;
		dropLuck = .75f;
		autoCollect = false;
		//TODO Create Dash behaviour (maybe not)
	}

	public override void _PhysicsProcess(double delta)
	{
		//Mouse direction relative to the Player - Need Player Global Position due to World Coordinates (Viewport mouse position would need a conversion)
		var mouseDistance = GetGlobalMousePosition() - GlobalPosition;
		mouseDirection = mouseDistance.Normalized();

		//Move camera according to mouse
		var offsetX = Mathf.Clamp(camera.Offset.X + mouseDirection.X, -10, 10);
		var offsetY = Mathf.Clamp(camera.Offset.Y + mouseDirection.Y, -10, 10);
		camera.Offset = new Vector2(offsetX, offsetY);
		// Player movement logic
		Vector2 velocity;
		Vector2 playerDirection = Input.GetVector("left", "right", "up", "down");
		if (playerDirection != Vector2.Zero)
		{
			velocity = playerDirection * speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, speed);
			// animation.Play("idle");
			if (!isAttacking)
			{
				animation.Stop();
			}
		}
		if (isAttacking)
		{
			velocity /= 2;
		}
		else //Walking Animations
		{
			EntityHelper.playAnimation(this, "walk");
		}
		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("melee_attack") && attackCooldown.IsStopped())
		{
			makeAttack(MELEE);
		}
		else if (@event.IsActionPressed("ranged_attack") && attackCooldown.IsStopped())
		{
			makeAttack(RANGED);
		}
	}

	private void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack"))
		{
			//Reseting speedscale for walk animation
			animation.SpeedScale = 1;
			isAttacking = false;
		}
	}

	private void swapWeaponVisibility(WeaponType type)
	{
		if (type == MELEE)
		{
			meleeWeapon.Monitoring = true;
			meleeWeapon.Visible = true;
			rangedWeapon.Visible = false;
		}
		else if (type == RANGED)
		{
			meleeWeapon.Monitoring = false;
			meleeWeapon.Visible = false;
			rangedWeapon.Visible = true;
		}
	}

	public void loadCharacter(Character c, Vector2 position)
	{
		//Player Data
		playerSprite.Texture = c.body;
		playerSpawn = position;
		Position = position;
		//Weapon Data
		var melee = c.meleeWeapon;
		var ranged = c.rangedWeapon;
		damage = new Vector2I(melee.damage, ranged.damage);
		meleeWeapon.GetNode<Sprite2D>("MeleeWeapon").Texture = melee.texture;
		rangedWeapon.Texture = c.rangedWeapon.texture;
		health = c.health;
		maxHealth = health;
		speed = c.speed;
		characterData = c;
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onPlayerHealthBarUpdate), health);
		finishedAnimation(false);
	}

	private void makeAttack(WeaponType type)
	{
		// Adjusting Animation Speed to adjust Player Attack speed
		// Diving current attack animation duration BY the attack speed timer 
		float animationDuration = Mathf.Lerp(.2f, .8f, (float)attackCooldown.WaitTime - .2f);
		animation.SpeedScale = .1f / animationDuration + 0.05f;
		EntityHelper.playAnimation(this, "attack");
		isAttacking = true;
		attackCooldown.Start();
		//Selecting Attack Type
		if (type == MELEE)
		{
			if (currentType != type)
			{
				swapWeaponVisibility(type);
				currentType = type;
			}
			AssetManager.instance.playSFX("meleeAttack");
		}
		else if (type == RANGED)
		{
			if (currentType != type)
			{
				swapWeaponVisibility(type);
				currentType = type;
			}
			var rangedWeapon = characterData.rangedWeapon;

			//Wall ricochets buff
			var wallRicochet = 0;
			var hitRicochet = 0;
			if (buffPool.ContainsKey(WALL_RICHOCHET))
			{
				wallRicochet = buffPool[WALL_RICHOCHET];
			}
			if (buffPool.ContainsKey(HIT_RICOCHET))
			{
				hitRicochet = buffPool[HIT_RICOCHET];
			}

			//Creating Unbuffed projectile
			if (!buffPool.ContainsKey(FRONTAL))
			{
				var projectile = PoolEngine.instance.pullFromPool<Projectile>();
				projectile.init(GlobalPosition, mouseDirection, mouseDirection.Angle(), this, rangedWeapon.projectileSpeed, rangedWeapon.angularSpeed, rangedWeapon.isProjectile, rangedWeapon.name, wallRicochet, hitRicochet);
			}

			//Creating Buffed projectile
			foreach (var b in buffPool)
			{
				switch (b.Key)
				{
					case FRONTAL:
						//Adjusting projectile offsets based on the number of projectiles
						for (int i = -5 * b.Value; i <= 5 * b.Value; i += 10)
						{
							var projectile = PoolEngine.instance.pullFromPool<Projectile>();
							var offset = new Vector2();
							//Perpendicular vector for offset
							offset = new Vector2(mouseDirection.Y, -mouseDirection.X) * i;
							projectile.init(GlobalPosition + offset, mouseDirection, mouseDirection.Angle(), this, rangedWeapon.projectileSpeed, rangedWeapon.angularSpeed, rangedWeapon.isProjectile, rangedWeapon.name, wallRicochet, hitRicochet);
						}
						break;
					case DIAGONAL:

						for (int i = -b.Value * 2; i <= b.Value * 2; i += 2)
						{
							if (i != 0)
							{
								var projectile = PoolEngine.instance.pullFromPool<Projectile>();
								//Creating new offsets based on current mouse direction angle
								var diagonal = mouseDirection.Angle() + Mathf.Pi / (i * 4 / b.Value);
								//Using COS and SIN to convert new Offset Angle to new vector X,Y
								var newDirection = new Vector2(Mathf.Cos(diagonal), Mathf.Sin(diagonal));
								projectile.init(GlobalPosition, newDirection, newDirection.Angle(), this, rangedWeapon.projectileSpeed, rangedWeapon.angularSpeed, rangedWeapon.isProjectile, rangedWeapon.name, wallRicochet, hitRicochet);
							}
						}
						break;
				}
			}
			AssetManager.instance.playSFX("rangedAttack");
		}
	}

	private void onMeleeAttackHit(Area2D area)
	{
		//Ensure the enemy was hit only once, checking enemies targeted by attack
		if (area.GetParent() is Enemy e && isAttacking && !enemiesMeleeTargeted.Contains(e))
		{
			var isCrit = EntityHelper.isCriticalHit(criticalChance);
			int dmg;
			if (isCrit)
			{
				dmg = (int)(damage.X * criticalDamage);
			}
			else
			{
				dmg = EntityHelper.getVariableDamage((int)damage.X);
			}
			e.takeDamage(dmg, isCrit);
			enemiesMeleeTargeted.Add(e);
		}
	}

	public void takeDamage(int damage)
	{
		EmitSignal(SignalName.onHealthChanged, -damage);
		health -= damage;
		health = Math.Max(0, health);
		//Hurt animation
		var tween = CreateTween();
		tween.TweenProperty(playerSprite, "self_modulate", Colors.DarkRed, .2f);
		tween.TweenProperty(playerSprite, "self_modulate", Colors.White, 0f);
		AssetManager.instance.playSFX("playerHit");
	}

	private void onAttackCooldownTimeout()
	{
		//Remove enemies targeted from list when animation finishes
		if (enemiesMeleeTargeted.Count != 0) enemiesMeleeTargeted.Clear();
		if (Input.IsActionPressed("melee_attack"))
		{
			makeAttack(MELEE);
		}
		else if (Input.IsActionPressed("ranged_attack"))
		{
			makeAttack(RANGED);
		}
	}
	private void autoCollectMode()
	{
		autoCollect = true;
	}

	//Switching level aniamtions
	public async void newLevelAnimation(bool arenaBoss)
	{
		//Deactivating player
		autoCollect = false;
		setPlayerProcess(false);
		Hide();
		//Animating going next level
		GD.Print("GOING NEW LEVEL");
		var tween = CreateTween();
		tween.TweenProperty(this, "position", Position + Vector2.Up * 50, 2);
		await tween.ToSignal(tween, "finished");
		finishedAnimation(arenaBoss);
		// tween.Connect("finished", Callable.From(() => finishedAnimation(arenaBoss)));

	}

	private async void finishedAnimation(bool arenaBoss)
	{
		//TODO Remove when char select avaiable
		setPlayerProcess(false);
		//Generating new level
		SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onGenerateLevel));
		AssetManager.instance.playSFX("doorTp");

		Show();
		GlobalPosition = playerSpawn;
		//Animating entry on new level
		var tween = CreateTween();
		animation.Play("walk_up");
		tween.TweenProperty(this, "position", Position + Vector2.Up * 50, 2);
		if (arenaBoss)
		{
			tween.TweenCallback(Callable.From(() =>
			{
				animation.Stop();
			}));
			tween.TweenProperty(camera, "offset", Vector2.Up * 200, 1f);
		}

		//After finished animation spawn enemies or wake up boss
		await tween.ToSignal(tween, "finished");
		if (LevelManager.level > 0)
		{
			setPlayerProcess(true);
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onSpawnEnemies));
		}
		else
		{   //Span to wake up boss
			await ToSignal(GetTree().CreateTimer(.75f), "timeout");
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onAwakeBoss));
		}

	}

	public void setPlayerProcess(bool value)
	{
		GD.Print("activating player");
		SetProcessInput(value);
		SetPhysicsProcess(value);
	}
}
