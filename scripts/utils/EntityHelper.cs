using Godot;
using System;
using Enums;
using static Enums.Direction;

public partial class EntityHelper : Node
{

    private const float rotation_factor = .4f;
    public static RandomNumberGenerator rnd = new RandomNumberGenerator();

    //Checks the mouse OR enemy positions relative to the player and return the equivalent Quadrant
    public static Direction getDirectionQuadrant(Vector2 direction)
    {
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            if (direction.X > 0)
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
            if (direction.Y > 0)
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
    // Need to negate some directions due to Vector Coordinates mappaed to Godot coordinates System: [0º(0PI) = RIGHT | 90º(PI/2) = DOWN]
    public static void playAnimation(Node2D entity, string animationType)
    {
        AnimationPlayer animation = null;
        var direction = Vector2.Zero;
        if (entity is Player p)
        {
            direction = p.mouseDirection;
            animation = p.animation;
        }
        else if (entity is Enemy e)
        {
            direction = e.enemyDirection;
            animation = e.animation;
        }

        var targetRotation = direction * rotation_factor;
        var selectedAnimation = "";
        switch (getDirectionQuadrant(direction))
        {
            case TOP:
                entity.Rotation = targetRotation.X;
                selectedAnimation = $"{animationType}_up";
                break;
            case RIGHT:
                entity.Rotation = targetRotation.Y;
                selectedAnimation = $"{animationType}_right";
                break;
            case DOWN:
                entity.Rotation = -targetRotation.X;
                selectedAnimation = $"{animationType}_down";
                break;
            case LEFT:
                entity.Rotation = -targetRotation.Y;
                selectedAnimation = $"{animationType}_left";
                break;
            default:
                GD.PrintErr("Invalid Direction to Move/Attack");
                break;
        }
        animation.Play(selectedAnimation);
    }

    public static int getVariableDamage(int damage)
    {
        var damageVariation = (int)(damage * 0.2f);
        return rnd.RandiRange(damage - damageVariation, damage + damageVariation);
    }

    public static bool isCriticalHit()
    {
        if (rnd.Randf() > .6f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void initEnemy(Enemy e, EnemyData data)
    {
        e.type = data.name;
        e.damage = data.damage;
        e.speed = data.speed;
        e.attackRange = data.range;
    }
}
