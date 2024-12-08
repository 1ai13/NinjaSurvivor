using Godot;
using System;

public partial class HudController : Control
{
	private TextureProgressBar healthBar;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		healthBar = GetNode<TextureProgressBar>("HealthBar");
		//TODO Improve HUD with ammo, PJ
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void onHealthChanged(int health)
	{
		healthBar.Value = health;
	}
}