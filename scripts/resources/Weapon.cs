using Godot;
using System;

[GlobalClass]
public partial class Weapon : Resource
{
    [Export]
    public string name { get; set; }
    [Export]
    public int damage { get; set; }
    [Export]
    public Texture2D[] textures { get; set; }
    [Export]
    public bool isProjectile { get; set; }
}