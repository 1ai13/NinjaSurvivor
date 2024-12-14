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

    public void applyBuff(Player p)
    {
        switch (type)
        {
            case HEALTH:
                p.maxHealth += 100;
                p.health += p.maxHealth;
                SignalBus.bus.EmitSignal("onHealthChanged", p.health);
                break;
            case DAMAGE:
                p.damage *= 1.1f;
                break;
            case ATTACK_SPEED:
                var attackSpeed = p.attackCooldown.WaitTime;
                p.attackCooldown.WaitTime = Mathf.Max(0.2f, attackSpeed - .1f);
                break;
        }
    }
}