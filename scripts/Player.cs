using Enums;
using Godot;
using System;
using static Enums.Direction;
using static Enums.WeaponType;

public partial class Player : CharacterBody2D
{

	[Export]
	private float speed = 100f;
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
	private bool canAttack = true;

	public override void _Ready()
	{
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		meleeWeapon = GetNode<Area2D>("Area2D");
		rangedWeapon = GetNode<Sprite2D>("RangeWeapon");
		projectileScene = GD.Load<PackedScene>("res://scenes/Projectile.tscn");
		playerSprite = GetNode<Sprite2D>("Body");
		attackCooldown = GetNode<Timer>("AttackCooldown");
		attackCooldown.Timeout += onAttackCooldownTimeout;
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
		if (@event.IsActionPressed("melee_attack") && canAttack)
		{
			makeAttack(MELEE);
		}
		else if (@event.IsActionPressed("ranged_attack") && canAttack)
		{
			makeAttack(RANGED);
		}
	}

	private void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack"))
		{
			isAttacking = false;
			meleeWeapon.Monitoring = false;
		}
	}

	//Checks the mouse position relative to the player and return the equivalent Quadrant
	private Direction getMouseQuadrant()
	{
		if (Math.Abs(mouseDirection.X) > Math.Abs(mouseDirection.Y))
		{
			if (mouseDirection.X > 0)
			{
				return RIGHT;
			}
			else
			{
				return LEFT;
			}
		}
		else
		{
			if (mouseDirection.Y > 0)
			{
				return DOWN;
			}
			else
			{
				return TOP;
			}
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
		//Projectile texture adjustment
		var auxProjectile = projectileScene.Instantiate<Projectile>();
		auxProjectile.isProjectile = ranged.isProjectile;
		auxProjectile.GetNode<Sprite2D>("ProjectileSprite").Texture = ranged.textures[1];
		var auxScene = new PackedScene();
		auxScene.Pack(auxProjectile);
		projectileScene = auxScene;
	}

	private void makeAttack(WeaponType type)
	{
		if (type == MELEE)
		{
			swapWeaponVisibility(type);
		}
		else if (type == RANGED)
		{
			swapWeaponVisibility(type);
			var projectile = projectileScene.Instantiate<Projectile>();
			projectile.init(GlobalPosition, mouseDirection, mouseDirection.Angle(), damage.Y);
			GetTree().CurrentScene.AddChild(projectile);
		}
		EntityHelper.playAnimation(this, "attack");
		isAttacking = true;
		canAttack = false;
		attackCooldown.Start();
	}

	private void onMeleeHit(Area2D area)
	{
		if (area.GetParent() is Enemy e && isAttacking)
		{
			GD.Print("Hitting enemy");
			e.takeDamage(damage.X);
		}
	}

	public void takeDamage(int damage)
	{
		health -= damage;
		//Blink animation
		var tween = CreateTween();
		tween.TweenProperty(playerSprite, "self_modulate", Colors.DarkRed, .2f);
		tween.TweenProperty(playerSprite, "self_modulate", Colors.White, 0f);
	}

	private void onAttackCooldownTimeout()
	{
		canAttack = true;
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
