using Godot;
using System;

[GlobalClass]
public partial class Character : Resource
{
    [Export]
    public string name { get; set; }
    [Export]
    public Texture2D body;
    [Export]
    public Weapon meleeWeapon;
    [Export]
    public Weapon rangedWeapon;
    [Export]
    public int health;
    [Export]
    public float speed;
}