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
	private float rotation_factor = .4f;
	private AnimationPlayer animation;
	private PackedScene projectileScene;
	private Area2D meleeWeapon;
	private Sprite2D rangedWeapon { get; set; }
	private bool isAttacking = false;
	private Vector2 mouseDirection;
	private Vector2 playerDamage;

	public override void _Ready()
	{
		animation = GetNode<AnimationPlayer>("AnimationPlayer");
		meleeWeapon = GetNode<Area2D>("Area2D");
		rangedWeapon = GetNode<Sprite2D>("RangeWeapon");
		projectileScene = GD.Load<PackedScene>("res://scenes/Projectile.tscn");
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
			playAnimation("walk");
		}
		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("melee_attack") && !isAttacking)
		{
			doAttack(MELEE);
		}
		else if (@event.IsActionPressed("ranged_attack") && !isAttacking)
		{
			doAttack(RANGED);
		}
	}

	private void onAnimationFinished(StringName animationName)
	{
		if (animationName.ToString().StartsWith("attack"))
		{
			isAttacking = false;
		}
		if (Input.IsActionPressed("melee_attack"))
		{
			doAttack(MELEE);
		}
		else if (Input.IsActionPressed("ranged_attack"))
		{
			doAttack(RANGED);
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

	// Top-Right-Left-Bottom Movement/Attack Animation + ROTATION
	// Need to negate some directions due to flipH property on Animations
	private void playAnimation(string animationType)
	{
		var targetRotation = mouseDirection * rotation_factor;
		switch (getMouseQuadrant())
		{
			case TOP:
				Rotation = targetRotation.X;
				animation.Play($"{animationType}_up");
				break;
			case RIGHT:
				Rotation = targetRotation.Y;
				animation.Play($"{animationType}_right");
				break;
			case DOWN:
				Rotation = -targetRotation.X;
				animation.Play($"{animationType}_down");
				break;
			case LEFT:
				Rotation = -targetRotation.Y;
				animation.Play($"{animationType}_left");
				break;
			default:
				GD.PrintErr("Invalid Direction to Move/Attack");
				break;
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
		GetNode<Sprite2D>("Body").Texture = c.body;
		Position = position;
		//Weapon Data
		var melee = c.meleeWeapon;
		var ranged = c.rangedWeapon;
		playerDamage = new Vector2(melee.damage, ranged.damage);
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

	private void doAttack(WeaponType type)
	{
		if (type == MELEE)
		{
			swapWeaponVisibility(type);
		}
		else if (type == RANGED)
		{
			swapWeaponVisibility(type);
			var projectile = projectileScene.Instantiate<Projectile>();
			projectile.init(GlobalPosition, mouseDirection, mouseDirection.Angle());
			GetTree().CurrentScene.AddChild(projectile);
		}
		isAttacking = true;
		playAnimation("attack");
	}
}
