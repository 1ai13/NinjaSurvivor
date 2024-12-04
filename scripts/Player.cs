using Enums;
using Godot;
using System;
using System.Collections.Generic;
using static Enums.WeaponType;

public partial class Player : CharacterBody2D
{

	[Signal]
	public delegate void healthChangedEventHandler(int health);
	[Export]
	private float speed = 60f;
	[Export]
	private int health { get; set; } = 100;
	public AnimationPlayer animation;
	private PackedScene projectileScene;
	private Area2D meleeWeapon;
	private Sprite2D rangedWeapon { get; set; }
	private bool isAttacking = false;
	public Vector2 mouseDirection { get; set; }
	private Vector2I damage { get; set; }
	private Sprite2D playerSprite;
	private Timer attackCooldown;
	private HashSet<Enemy> enemiesMeleeTargeted;
	private WeaponType currentType = MELEE;

	public override void _Ready()
	{
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		meleeWeapon = GetNode<Area2D>("Area2D");
		rangedWeapon = GetNode<Sprite2D>("RangeWeapon");
		projectileScene = AssetManager.instance.projectileScene;
		playerSprite = GetNode<Sprite2D>("Body");
		enemiesMeleeTargeted = new HashSet<Enemy>();
		attackCooldown = GetNode<Timer>("AttackCooldown");
		attackCooldown.Timeout += onAttackCooldownTimeout;
		EmitSignal(SignalName.healthChanged, health);
		//TODO Create Dash behaviour
	}
	public override void _PhysicsProcess(double delta)
	{
		//Mouse direction relative to the Player - Need Player Global Position due to World Coordinates (Viewport mouse position would need a conversion)
		mouseDirection = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		// Player movement logic
		Vector2 velocity = Velocity;
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
		Position = position;
		//Weapon Data
		var melee = c.meleeWeapon;
		var ranged = c.rangedWeapon;
		damage = new Vector2I(melee.damage, ranged.damage);
		meleeWeapon.GetNode<Sprite2D>("MeleeWeapon").Texture = melee.textures[0];
		rangedWeapon.Texture = c.rangedWeapon.textures[0];
		//Projectile texture adjustment, need to initialize the scene and pack it again for future use
		var auxProjectile = projectileScene.Instantiate<Projectile>();
		auxProjectile.isProjectile = ranged.isProjectile;
		auxProjectile.GetNode<Sprite2D>("ProjectileSprite").Texture = ranged.textures[1];
		var auxScene = new PackedScene();
		auxScene.Pack(auxProjectile);
		projectileScene = auxScene;
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
			var projectile = projectileScene.Instantiate<Projectile>();
			projectile.init(GlobalPosition, mouseDirection, mouseDirection.Angle(), damage.Y, this);
			projectile.projectileHit += onRangedEnemyHit;
			GetTree().CurrentScene.AddChild(projectile);
			AssetManager.instance.playSFX("rangedAttack");
		}
	}

	private void onMeleeAttackHit(Area2D area)
	{
		//Ensure the enemy was hit only once, checking enemies targeted by attack
		if (area.GetParent() is Enemy e && isAttacking && !enemiesMeleeTargeted.Contains(e))
		{
			var isCrit = EntityHelper.isCriticalHit();
			int dmg;
			if (isCrit)
			{
				dmg = (int)(damage.X * 1.6f);
			}
			else
			{
				dmg = EntityHelper.getVariableDamage(damage.X);
			}
			e.takeDamage(dmg, isCrit);
			enemiesMeleeTargeted.Add(e);
		}
	}
	private void onRangedEnemyHit(Area2D area)
	{
		if (area.GetParent() is Enemy e)
		{
			var isCrit = EntityHelper.isCriticalHit();
			int dmg;
			if (isCrit)
			{
				dmg = (int)(damage.Y * 1.6f);
			}
			else
			{
				dmg = EntityHelper.getVariableDamage(damage.Y);
			}
			e.takeDamage(dmg, isCrit);
		}
	}

	public void takeDamage(int damage)
	{
		health -= damage;
		//Hurt animation
		var tween = CreateTween();
		tween.TweenProperty(playerSprite, "self_modulate", Colors.DarkRed, .2f);
		tween.TweenProperty(playerSprite, "self_modulate", Colors.White, 0f);
		AssetManager.instance.playSFX("playerHit");
		EmitSignal(SignalName.healthChanged, health);
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
}
