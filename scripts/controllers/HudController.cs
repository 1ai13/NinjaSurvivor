using Godot;
using Godot.Collections;
using System;
using System.Linq;
public partial class HudController : Control
{
	private const string smallFontCounter = "[font_size=8]x[/font_size]";
	private TextureProgressBar healthBar;
	private HFlowContainer healthBarContainer;
	private VBoxContainer buffsContainer;
	private Panel modalContainer;
	private HBoxContainer modalBuffsContainer;
	private RichTextLabel levelCounter;
	private RichTextLabel waveCounter;
	private RichTextLabel goldCounter;
	public Label notification;
	private int numberOfHearts;
	private Tween tween;
	private System.Collections.Generic.Dictionary<Buff, int> buffsApplied;
	private Array<Buff> currentRandomBuffs;
	private Player player;
	private BufferController buffer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		healthBarContainer = GetNode<HFlowContainer>("HealthBarContainer");
		buffsContainer = GetNode<VBoxContainer>("BuffsContainer");
		modalContainer = GetNode<Panel>("BuffModal");
		modalBuffsContainer = GetNode<HBoxContainer>("BuffModal/BuffsContainer");
		levelCounter = GetNode<RichTextLabel>("LevelInfo/LevelLabel");
		waveCounter = GetNode<RichTextLabel>("LevelInfo/WaveLabel");
		goldCounter = GetNode<RichTextLabel>("GoldCounter/GoldCounter");
		buffsApplied = new System.Collections.Generic.Dictionary<Buff, int>();
		notification = GetNode<Label>("NotificationLabel");
		player = GetTree().CurrentScene.GetNode<Player>("Player");
		buffer = GetTree().CurrentScene.GetNode<BufferController>("Buffer");
		SignalBus.bus.onNotifyPlayer += showNotification;
		SignalBus.bus.onPlayerHealthBarUpdate += playerHealthBarUpdate;
		SignalBus.bus.onLevelCompleted += levelCompletedUpdate;
		SignalBus.bus.onWaveCompleted += waveCompletedUpdate;
		SignalBus.bus.onCoinCollected += coinCollected;
		//TODO Improve HUD with ammo, dynamic healthbar
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void healthChanged(int value)
	{
		//Least optimal way of updating healthbar
		// var hp = player.health;
		// for (int i = 0; i < healthBarContainer.GetChildCount(); i++)
		// {
		// 	var healthBar = healthBarContainer.GetChild<TextureProgressBar>(i);
		// 	if (hp > 0)
		// 	{
		// 		if (hp >= 100)
		// 		{
		// 			healthBar.Value = 100;
		// 			hp -= 100;
		// 		}
		// 		else
		// 		{
		// 			healthBar.Value = hp;
		// 			this.healthBar = healthBarContainer.GetChild<TextureProgressBar>(i);
		// 			hp = 0;
		// 		}
		// 	}
		// 	else
		// 	{
		// 		healthBar.Value = 0;
		// 	}
		// }
		int remainingValue = Mathf.Abs(value);
		if (value < 0)
		{
			while (remainingValue > 0 && healthBar.GetIndex() >= 0)
			{
				if (remainingValue >= healthBar.Value && healthBar.GetIndex() != 0)
				{
					remainingValue -= (int)healthBar.Value;
					healthBar.Value = 0;
					healthBar = healthBarContainer.GetChild<TextureProgressBar>(healthBar.GetIndex() - 1);
				}
				else
				{
					healthBar.Value -= remainingValue;
					remainingValue = 0;
				}
			}
		}
		else if (value > 0)
		{
			while (remainingValue > 0 && healthBar.GetIndex() < healthBarContainer.GetChildCount())
			{
				if (healthBar.Value + remainingValue > 100 && healthBar.GetIndex() != healthBarContainer.GetChildCount() - 1)
				{
					remainingValue -= (int)(100 - healthBar.Value);
					healthBar.Value = 100;
					healthBar = healthBarContainer.GetChild<TextureProgressBar>(healthBar.GetIndex() + 1);
				}
				else
				{
					healthBar.Value += remainingValue;
					remainingValue = 0;
				}
			}
		}

	}

	private void coinCollected()
	{
		goldCounter.Text = $"{player.gold}[font_size=8] g[/font_size]";
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

	private void playerBuffAdded(Buff buff)
	{
		//Creating icon and counter
		var buffStat = AssetManager.instance.buffStatCounter.Instantiate<HFlowContainer>();
		var icon = buffStat.GetNode<TextureRect>("Icon");
		var label = buffStat.GetNode<RichTextLabel>("Counter");
		icon.Texture = buff.icons[0];
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

	private void randomBuffsGenerated(Array<Buff> randomBuffs)
	{
		currentRandomBuffs = randomBuffs;
		//Clearing current buffs
		modalBuffsContainer.GetChildren().ToList().ForEach(x =>
		{
			x.QueueFree();
		});
		//Assigning button images and description
		for (int i = 0; i < randomBuffs.Count; i++)
		{
			var buff = AssetManager.instance.buffRandomBuff.Instantiate<TextureButton>();
			buff.TextureNormal = randomBuffs[i].icons[1];
			buff.TextureHover = randomBuffs[i].icons[2];
			buff.TexturePressed = randomBuffs[i].icons[3];
			buff.GetNode<Label>("BuffDescription").Text = randomBuffs[i].description;
			switch (i)
			{
				case 0:
					buff.Pressed += firstBuffSelected;
					break;
				case 1:
					buff.Pressed += secondBuffSelected;
					break;
				case 2:
					buff.Pressed += thirdBuffSelected;
					break;
			}
			modalBuffsContainer.AddChild(buff);
		}
		modalContainer.Visible = true;
	}

	private void firstBuffSelected()
	{
		buffSelected(0);
	}

	private void secondBuffSelected()
	{
		buffSelected(1);
	}

	private void thirdBuffSelected()
	{
		buffSelected(2);
	}

	private void buffSelected(int index)
	{
		//Applying buff and hiding modal
		buffer.bufferBubble.Play("sleep");
		player.SetPhysicsProcess(true);
		player.SetProcessInput(true);
		modalContainer.Visible = false;
		buffsContainer.Visible = true;
		var maxedBuff = currentRandomBuffs[index].applyBuff(player);
		playerBuffAdded(currentRandomBuffs[index]);
		//Maxed buff
		if (maxedBuff)
		{
			buffer.buffs.Remove(currentRandomBuffs[index]);
		}
	}
}