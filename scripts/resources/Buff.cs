using Enums;
using Godot;
using System;
using static Enums.BuffType;

[GlobalClass]
public partial class Buff : Resource
{
    [Export]
    public BuffType type;
    [Export]
    public Texture2D[] icons;
    [Export]
    public string description;

    public bool applyBuff(Player p)
    {
        var maxed = false;
        if (!p.buffPool.ContainsKey(type))
        {
            p.buffPool.Add(type, 1);

        }
        else
        {
            p.buffPool[type]++;
        }
        switch (type)
        {
            case HEALTH:

                p.maxHealth = Math.Min(4400, p.maxHealth + 100);
                p.health = p.maxHealth;
                if (p.maxHealth == 4400)
                {
                    GD.Print("Maxed");
                    maxed = true;
                }
                SignalBus.bus.EmitSignal("onPlayerHealthBarUpdate", p.health);
                break;
            case DAMAGE:
                p.damage *= 1.1f;
                break;
            case ATTACK_SPEED:
                p.attackCooldown.WaitTime = Math.Max(p.attackCooldown.WaitTime - .1f, .2f);
                if (p.attackCooldown.WaitTime == .2f)
                {
                    GD.Print("Maxed");
                    maxed = true;
                }
                break;
            case MOVEMENT_SPEED:
                p.speed = Math.Min(p.speed + 5, 100);
                if (p.speed == 100)
                {
                    GD.Print("Maxed");
                    maxed = true;
                }
                break;
            case FRONTAL:
                if (p.buffPool[type] == 2)
                {
                    GD.Print("Maxed");
                    maxed = true;
                }
                break;
            case WALL_RICHOCHET:
                if (p.buffPool[type] == 3)
                {
                    maxed = true;
                    GD.Print("Maxed");
                }
                break;
            case DIAGONAL:
                if (p.buffPool[type] == 2)
                {
                    GD.Print("Maxed");
                    maxed = true;
                }
                break;
            case HIT_RICOCHET:
                if (p.buffPool[type] == 3)
                {
                    maxed = true;
                    GD.Print("Maxed");
                }
                break;
        }
        return maxed;
    }
}