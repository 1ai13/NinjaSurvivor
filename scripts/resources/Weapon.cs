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
    public Texture2D texture { get; set; }
    [Export]
    public Texture2D icon { get; set; }
    [Export]
    public bool isProjectile { get; set; }
    [Export]
    public float projectileSpeed { get; set; }
    [Export]
    public float angularSpeed { get; set; }
}