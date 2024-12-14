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
    public Texture2D icon;
    [Export]
    public string description;
    [Export]
    public bool isDirect;

    public bool applyBuff(Player p)
    {
        switch (type)
        {
            case HEALTH:
                p.maxHealth += 100;
                p.health = p.maxHealth;
                SignalBus.bus.EmitSignal("onPlayerHealthBarUpdate", p.health);
                break;
            case DAMAGE:
                p.damage *= 1.1f;
                break;
            case ATTACK_SPEED:
                p.attackCooldown.WaitTime = p.attackCooldown.WaitTime - .1f;
                if (p.attackCooldown.WaitTime == .2f)
                {
                    return true;
                }
                break;
        }
        return false;
    }
}