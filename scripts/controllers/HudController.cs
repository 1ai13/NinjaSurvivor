using Godot;
using System;

public partial class HudController : Control
{
	private TextureProgressBar healthBar;
	public Label notification;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		healthBar = GetNode<TextureProgressBar>("HealthBar");
		notification = GetNode<Label>("NotificationLabel");
		SignalBus.bus.onNotifyPlayer += showNotification;
		//TODO Improve HUD with ammo, dynamic healthbar
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void onHealthChanged(int health)
	{
		healthBar.Value = health;
	}

	//Notify player and animates de message
	private void showNotification(string message, Color color)
	{
		notification.Text = message;
		var tween = this.CreateTween();
		tween.TweenProperty(notification, "visible", true, .75f);
		tween.TweenProperty(notification, "self_modulate", color, .5f);
		tween.TweenProperty(notification, "visible", true, 1);
		tween.TweenProperty(notification, "self_modulate", Color.Color8(1, 1, 1, 0), .5f);
		tween.TweenProperty(notification, "visible", true, 0);
	}
}