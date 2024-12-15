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

    public bool applyBuff(Player p)
    {
        var maxed = false;
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
                p.attackCooldown.WaitTime -= .1f;
                GD.Print("Current AS" + p.attackCooldown.WaitTime);
                if (p.attackCooldown.WaitTime <= .2f)
                {
                    p.attackCooldown.WaitTime = .2f;
                    GD.Print("Maxed");
                    maxed = true;
                }
                break;
            case FRONTAL:
                if (!p.buffPool.ContainsKey(this))
                {
                    p.buffPool.Add(this, 1);

                }
                else
                {
                    GD.Print("Maxed");
                    maxed = true;
                    p.buffPool[this] = 2;
                }
                break;
        }
        return maxed;
    }
}