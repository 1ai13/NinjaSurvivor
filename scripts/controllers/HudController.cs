using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class HudController : Control
{
	private TextureProgressBar healthBar;
	private HFlowContainer healthBarContainer;
	public Label notification;
	private int numberOfHearts;
	private Tween tween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		healthBarContainer = GetNode<HFlowContainer>("HealthBarContainer");
		notification = GetNode<Label>("NotificationLabel");
		SignalBus.bus.onNotifyPlayer += showNotificationAsync;
		SignalBus.bus.onHealthChanged += healthChanged;
		SignalBus.bus.onPlayerHealthBarUpdate += playerHealthBarUpdate;
		//TODO Improve HUD with ammo, dynamic healthbar
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void healthChanged(int health)
	{
		//Health of the current Heart
		int playerHealth;
		playerHealth = health - 100 * (numberOfHearts - 1);
		//Swap active Heart if Player loses HP
		if (playerHealth <= 0 && health >= 100)
		{
			healthBar.Value = 0;
			healthBar = healthBarContainer.GetChild<TextureProgressBar>(healthBar.GetIndex() - 1);
			healthBar.Value -= playerHealth;
			GD.Print("Swapped hp value2" + healthBar.Value);
			numberOfHearts--;
		}
		else
		{
			healthBar.Value = playerHealth;
		}
		GD.Print(healthBar.Value);
	}

	private void playerHealthBarUpdate(int health)
	{
		//Generate Hearts initializing or buffing player
		healthBarContainer.GetChildren().ToList().ForEach(x =>
		{
			x.QueueFree();
		});
		numberOfHearts = health / 100;
		for (int i = 0; i < numberOfHearts; i++)
		{
			var hBar = AssetManager.instance.playerHealthBarScene.Instantiate<TextureProgressBar>();
			hBar.Value = 100;
			healthBarContainer.AddChild(hBar);
			GD.Print("Creating heart");
		}
		healthBar = healthBarContainer.GetChild<TextureProgressBar>(healthBarContainer.GetChildCount() - 1);
	}

	//Notify player and animates de message
	private async void showNotificationAsync(string message, Color color)
	{
		if (tween != null && tween.IsRunning())
		{
			await ToSignal(tween, "finished");
		}
		tween = this.CreateTween();
		notification.Text = message;
		tween.TweenProperty(notification, "visible", true, .5f);
		tween.TweenProperty(notification, "self_modulate", color, .5f);
		tween.TweenProperty(notification, "visible", true, 1);
		tween.TweenProperty(notification, "self_modulate", Color.Color8(1, 1, 1, 0), .5f);
		tween.TweenProperty(notification, "visible", true, 0);
	}
}