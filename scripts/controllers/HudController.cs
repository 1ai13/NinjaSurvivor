using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class HudController : Control
{
	private const string smallFontCounter = "[font_size=8]x[/font_size]";
	private TextureProgressBar healthBar;
	private HFlowContainer healthBarContainer;
	private VBoxContainer buffsContainer;
	private RichTextLabel levelCounter;
	private RichTextLabel waveCounter;
	public Label notification;
	private int numberOfHearts;
	private Tween tween;
	private Dictionary<Buff, int> buffsApplied;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		healthBarContainer = GetNode<HFlowContainer>("HealthBarContainer");
		buffsContainer = GetNode<VBoxContainer>("BuffsContainer");
		levelCounter = GetNode<RichTextLabel>("LevelInfo/LevelLabel");
		waveCounter = GetNode<RichTextLabel>("LevelInfo/WaveLabel");
		buffsApplied = new Dictionary<Buff, int>();
		notification = GetNode<Label>("NotificationLabel");
		SignalBus.bus.onNotifyPlayer += showNotification;
		SignalBus.bus.onHealthChanged += healthChanged;
		SignalBus.bus.onPlayerHealthBarUpdate += playerHealthBarUpdate;
		SignalBus.bus.onPlayerBuffed += playerBuffAdded;
		SignalBus.bus.onLevelCompleted += levelCompletedUpdate;
		SignalBus.bus.onWaveCompleted += waveCompletedUpdate;
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
		}
		healthBar = healthBarContainer.GetChild<TextureProgressBar>(healthBarContainer.GetChildCount() - 1);
	}

	//Notify player and animates de message
	private async void showNotification(string message, Color color)
	{
		if (tween != null && tween.IsRunning())
		{
			await ToSignal(tween, "finished");
		}
		tween = this.CreateTween();
		notification.Text = message;
		tween.TweenProperty(notification, "visible", true, .3f);
		tween.TweenProperty(notification, "self_modulate", color, .5f);
		tween.TweenProperty(notification, "visible", true, 1);
		tween.TweenProperty(notification, "self_modulate", Color.Color8(1, 1, 1, 0), .5f);
		tween.TweenProperty(notification, "visible", true, 0);
	}

	private void playerBuffAdded(Buff buff, Player p)
	{
		//Creating icon and counter
		var buffStat = AssetManager.instance.buffStatCounter.Instantiate<HFlowContainer>();
		var icon = buffStat.GetNode<TextureRect>("Icon");
		var label = buffStat.GetNode<RichTextLabel>("Counter");
		icon.Texture = buff.icons[1];
		buffStat.Name += $"-{buff.type}";
		//New buff
		if (!buffsApplied.ContainsKey(buff))
		{
			label.Text = smallFontCounter + $" {1}";
			buffsContainer.AddChild(buffStat);
			buffsApplied.Add(buff, 1);
		}
		else //Already placed buff
		{
			buffsApplied[buff]++;
			var buffCounter = buffsContainer.GetNode<RichTextLabel>("Buff-" + buff.type + "/Counter");
			buffCounter.Text = label.Text = smallFontCounter + $" {buffsApplied[buff]}";
			//Annoying bug not SORTING correctly
			// string buffSamePath = null;
			// string buffBiggerPath = null;
			// var leastBigger = Mathf.Inf;
			// foreach (var b in buffsApplied)
			// {
			// 	GD.Print("Comparing values: " + b.Value + " " + buffsApplied[buff]);
			// 	if (b.Key.type != buff.type)
			// 	{
			// 		if (b.Value == buffsApplied[buff])
			// 		{
			// 			GD.Print("Found same");
			// 			buffSamePath = b.Key.type.ToString();
			// 		}
			//BUG HERE not enusring the biggesr values is the last in the buff list
			// 		else if (b.Value > buffsApplied[buff] && buffSamePath == null && b.Value <= leastBigger)
			// 		{
			// 			GD.Print("Found bigger");
			// 			leastBigger = b.Value;
			// 			buffBiggerPath = b.Key.type.ToString();

			// 		}
			// 	}

			// }
			// if (buffsApplied.Count > 1)
			// {
			// 	if (buffSamePath != null)
			// 	{
			// 		GD.Print("Found same moving under");
			// 		var index = buffsContainer.GetNode<HFlowContainer>("Buff-" + buffSamePath).GetIndex();
			// 		buffsContainer.MoveChild(buffCounter.GetParent(), index + 1);
			// 	}
			// 	else if (buffBiggerPath != null)
			// 	{
			// 		GD.Print("Found bigger moving under");
			// 		var index = buffsContainer.GetNode<HFlowContainer>("Buff-" + buffBiggerPath).GetIndex();
			// 		buffsContainer.MoveChild(buffCounter.GetParent(), index + 1);
			// 	}
			// 	else
			// 	{
			// 		GD.Print("Maxing");
			// 		buffsContainer.MoveChild(buffCounter.GetParent(), 1);
			// 	}
			// }
		}
	}
	private void levelCompletedUpdate(int level, string icon)
	{
		levelCounter.Text = $"[right]LEVEL\t{level} - [img=12]{icon}[/img][/right]";
	}
	private void waveCompletedUpdate(int wave)
	{
		waveCounter.Text = $"WAVE\t\t\t\t\t\t{wave}  /  5";
	}
}