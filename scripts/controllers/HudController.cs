using Godot;
using Godot.Collections;
using System;
using System.Data.Common;
using System.Linq;
public partial class HudController : Control
{
	[Signal]
	public delegate void onSaveGameEventHandler();
	[Signal]
	public delegate void onSaveConfigEventHandler();
	private const string smallFontCounter = "[font_size=8]x[/font_size]";
	private TextureProgressBar healthBar;
	private HFlowContainer healthBarContainer;
	private VBoxContainer buffsContainer;
	private Panel modalContainer;
	private HBoxContainer modalBuffsContainer;
	private VFlowContainer bossHealthBarContainer;
	private TextureRect bossIcon;
	private ProgressBar baseBossHealthBar;
	private ProgressBar bossHealthBar;
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
	private TextureButton activeScroll;
	private Panel scrollModal;
	private TextureRect scrollIcon;
	private Label scrollTitle;
	private HBoxContainer scrollsContainer;
	private bool escapePressed;
	private Panel gameOverModal;
	public ProgressBar soundFXSlider;
	public Panel pausedModal;
	public ProgressBar pausedFXSlider;
	public float lastSoundFXVolume;
	public int SFXBusIndex;
	private bool minusSFX;
	private bool plusSFX;
	private float sliderSpeed;
	private GameController game;

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		game = GetNode<GameController>("/root/Game");
		healthBarContainer = GetNode<HFlowContainer>("PlayerHealthBarContainer");
		buffsContainer = GetNode<VBoxContainer>("BuffsContainer");
		modalContainer = GetNode<Panel>("BuffModal");
		modalBuffsContainer = GetNode<HBoxContainer>("BuffModal/BuffsContainer");
		bossHealthBarContainer = GetNode<VFlowContainer>("BossHealthBarContainer");
		bossIcon = bossHealthBarContainer.GetNode<TextureRect>("BossIcon");
		baseBossHealthBar = bossHealthBarContainer.GetNode<ProgressBar>("BaseBossHealthBar");
		bossHealthBar = baseBossHealthBar.GetNode<ProgressBar>("BossHealthBar");
		levelCounter = GetNode<RichTextLabel>("LevelInfo/LevelLabel");
		waveCounter = GetNode<RichTextLabel>("LevelInfo/WaveLabel");
		goldCounter = GetNode<RichTextLabel>("GoldCounter/GoldCounter");
		buffsApplied = new System.Collections.Generic.Dictionary<Buff, int>();
		notification = GetNode<Label>("NotificationLabel");
		activeScroll = GetNode<TextureButton>("ActiveScrollButton");
		scrollModal = GetNode<Panel>("ScrollModal");
		scrollIcon = GetNode<TextureRect>("ScrollModal/ScrollIcon");
		scrollTitle = GetNode<Label>("ScrollModal/Title");
		scrollsContainer = GetNode<HBoxContainer>("ScrollsContainer");
		gameOverModal = GetNode<Panel>("GameOverModal");
		soundFXSlider = GetNode<ProgressBar>("GameOverModal/SoundFXVolume");
		pausedModal = GetNode<Panel>("PauseModal");
		pausedFXSlider = GetNode<ProgressBar>("PauseModal/SoundFXVolume");
		lastSoundFXVolume = (float)soundFXSlider.Value;
		sliderSpeed = 10;
		SFXBusIndex = AudioServer.GetBusIndex("SFX");
		player = GetNode<Player>("/root/Game/Player");
		buffer = GetNode<BufferController>("/root/Game/Buffer");
		SignalBus.bus.onNotifyPlayer += showNotification;
		SignalBus.bus.onPlayerHealthBarUpdate += playerHealthBarUpdate;
		SignalBus.bus.onLevelCompleted += levelCompletedUpdate;
		SignalBus.bus.onWaveCompleted += waveCompletedUpdate;
		SignalBus.bus.onCoinCollected += coinCollected;
		SignalBus.bus.onBossReady += setBossHealthbar;
		SignalBus.bus.onBossHit += updateBossHealthbar;
		SignalBus.bus.onScrollCollected += scrollCollected;
		SignalBus.bus.onGameOver += gameOver;
		SignalBus.bus.onScrollUpdate += updateActiveScroll;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Escape) && !escapePressed)
		{
			escapePressed = true;
			if (scrollModal.Visible)
			{
				scrollModal.Hide();
			}
			else if (modalContainer.Visible)
			{
				modalContainer.Hide();
				player.setPlayerProcess(true);
			}
			else if (!pausedModal.Visible && !gameOverModal.Visible)
			{
				pausedModal.Visible = true;
				GetTree().Paused = true;
			}
			else if (pausedModal.Visible)
			{
				pausedModal.Visible = false;
				GetTree().Paused = false;
			}
		}
		else if (!Input.IsKeyPressed(Key.Escape))
		{
			escapePressed = false;
		}
		if (plusSFX)
		{
			sliderSpeed = Mathf.Min(40, sliderSpeed + (float)delta * 50);
			lastSoundFXVolume += .01f * sliderSpeed * (float)delta;
			lastSoundFXVolume = Math.Min(1, lastSoundFXVolume);
			AudioServer.SetBusVolumeDb(SFXBusIndex, Mathf.LinearToDb(lastSoundFXVolume));
			pausedFXSlider.Value = lastSoundFXVolume;
			soundFXSlider.Value = lastSoundFXVolume;
		}
		else if (minusSFX)
		{
			sliderSpeed = Mathf.Min(40, sliderSpeed + (float)delta * 50);
			lastSoundFXVolume -= .01f * sliderSpeed * (float)delta; ;
			lastSoundFXVolume = Math.Max(0, lastSoundFXVolume);
			AudioServer.SetBusVolumeDb(SFXBusIndex, Mathf.LinearToDb(lastSoundFXVolume));
			pausedFXSlider.Value = lastSoundFXVolume;
			soundFXSlider.Value = lastSoundFXVolume;
		}
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
		goldCounter.Text = $"{player.gold}[font_size=8]  g[/font_size]";
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
		var duration = 1;
		if (tween != null && tween.IsRunning())
		{
			await ToSignal(tween, "finished");
		}
		if (message.StartsWith("DEMO"))
		{
			AssetManager.instance.playSFX(GD.Load<AudioStream>("res://assets/audio/ui/victoryFX.wav"));
			player.setPlayerProcess(false);
			player.animation.CallDeferred("stop");
			duration = 3;
		}
		if (message.StartsWith("GAME"))
		{
			AssetManager.instance.playSFX("gameOver");
			player.setPlayerProcess(false);
			player.animation.CallDeferred("stop");
			duration = 2;
		}
		tween = this.CreateTween();
		notification.Text = message;
		tween.TweenProperty(notification, "visible", true, .3f);
		tween.TweenProperty(notification, "self_modulate", color, .5f);
		tween.TweenProperty(notification, "visible", true, duration);
		tween.TweenProperty(notification, "self_modulate", Color.Color8(1, 1, 1, 0), .5f);
		tween.TweenProperty(notification, "visible", true, 0);
		if (message.StartsWith("DEMO") || message.StartsWith("GAME"))
		{
			await ToSignal(tween, "finished");
			gameOverModal.Visible = true;
		}
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
		if (level == -1)
		{
			waveCounter.Visible = false;
			levelCounter.Text = $"[right]LEVEL\t[img=12]res://assets/textures/GUI/HUD/bossIcon.png[/img] - [img=12]{icon}[/img][/right]";
		}
		else
		{
			waveCounter.Visible = true;
			levelCounter.Text = $"[right]LEVEL\t{level} - [img=12]{icon}[/img][/right]";
		}
	}

	private void waveCompletedUpdate(int wave, int maxWaves)
	{
		waveCounter.Text = $"WAVE\t\t\t\t\t\t{wave}  /  {maxWaves}";
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
			buff.Connect("mouse_entered", Callable.From(mouseHoveringButton));
			modalBuffsContainer.AddChild(buff);
		}
		modalContainer.Visible = true;
	}

	private void mouseHoveringButton()
	{
		AssetManager.instance.playSFX("buttonHover");
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
		buffer.alive = false;
		//Applying buff and hiding modal
		buffer.bufferBubble.Play("sleep");
		AssetManager.instance.playSFX("closeBuffer");
		player.setPlayerProcess(true);
		modalContainer.Visible = false;
		if (!buffsContainer.Visible)
		{
			buffsContainer.Show();
		}
		var maxedBuff = currentRandomBuffs[index].applyBuff(player);
		playerBuffAdded(currentRandomBuffs[index]);
		//Maxed buff
		if (maxedBuff)
		{
			buffer.buffs.Remove(currentRandomBuffs[index]);
		}
	}

	private void setBossHealthbar(bool value, int health = 0)
	{
		baseBossHealthBar.MaxValue = health;
		baseBossHealthBar.Value = health;
		bossHealthBar.MaxValue = health;
		bossHealthBar.Value = health;
		bossHealthBar.Modulate = Colors.Red;
		var tween = CreateTween();
		if (value)
		{
			tween.TweenProperty(bossHealthBarContainer, "modulate", Colors.White, .7f);
		}
	}

	private void updateBossHealthbar(int health)
	{
		bossHealthBar.Value = health;
		var tween = CreateTween();
		tween.TweenProperty(baseBossHealthBar, "value", health, .4f);
		if (health == 0)
		{
			tween.Finished += () =>
			{
				var tween = CreateTween();
				tween.TweenProperty(bossHealthBarContainer, "modulate", Color.Color8(1, 1, 1, 0), .5f);
			};
		}
	}

	private void scrollCollected()
	{
		player.scrollsCollected++;
		switch (LevelManager.currentBiome - 1)
		{
			case 0:
				activeScroll.TextureNormal = AssetManager.instance.plantScroll;
				scrollIcon.Texture = AssetManager.instance.plantScroll;
				scrollTitle.Text = scrollTitle.Text.Replace("{SCROLL_NAME}", "LEAF");
				player.activeScroll = 1;
				break;
		}
		if (activeScroll.Disabled) activeScroll.Disabled = false;
		scrollModal.Visible = true;
		EmitSignal(SignalName.onSaveGame);
	}

	private void scrollButtonPressed()
	{
		if (scrollsContainer.Visible)
		{
			scrollsContainer.Hide();
		}
		else
		{
			scrollsContainer.Show();
		}
	}
	private void plantScrollSelected()
	{
		activeScroll.TextureNormal = AssetManager.instance.plantScroll;
		scrollsContainer.Hide();
	}

	private void gameOver()
	{
		EmitSignal(SignalName.onSaveGame);
		if (player.health == 0)
		{
			showNotification("GAME   OVER", Colors.Red);
		}
		else
		{
			showNotification("DEMO   FINISHED\nTHANKS   FOR   PLAYING\nFEEL   FREE   TO   GIVE   FEEDBACK   :)", Colors.White);
		}
	}

	private void switchSoundFXVolume(bool toggle)
	{
		if (!toggle)
		{
			AudioServer.SetBusVolumeDb(SFXBusIndex, Mathf.LinearToDb(lastSoundFXVolume));
			soundFXSlider.Value = lastSoundFXVolume;
			pausedFXSlider.Value = lastSoundFXVolume;
		}
		else
		{
			lastSoundFXVolume = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(SFXBusIndex));
			AudioServer.SetBusVolumeDb(SFXBusIndex, -80);
			soundFXSlider.Value = 0;
			pausedFXSlider.Value = 0;
		}
	}

	private void decreaseSoundFXVolume()
	{
		sliderSpeed = 10;
		minusSFX = !minusSFX;
	}

	private void increaseSoundFXVolume()
	{
		sliderSpeed = 10;
		plusSFX = !plusSFX;
	}
	private void restartPressed()
	{
		LevelManager.level = -1;
		LevelManager.currentBiome = 0;
		buffer.buffs = buffer.buffPool;
		player.loadCharacter(player.characterData, player.playerSpawn);
		buffsContainer.Visible = false;
		buffsContainer.GetChildren().OfType<HFlowContainer>().ToList().ForEach(x => buffsContainer.RemoveChild(x));
		gameOverModal.Visible = false;
		bossHealthBarContainer.Visible = false;
	}

	private void updateActiveScroll()
	{
		switch (player.activeScroll)
		{
			case 1:
				activeScroll.TextureNormal = AssetManager.instance.plantScroll;
				activeScroll.Disabled = false;
				break;
		}
	}
	private void modalHidden()
	{
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err != Error.Ok)
		{
			return;
		}
		var volume = (float)statsFile.GetValue("game", "soundFXVolume");
		if (volume != (float)pausedFXSlider.Value)
		{
			EmitSignal(SignalName.onSaveConfig);
		}
	}

	private void unpauseGame()
	{
		pausedModal.Visible = false;
		GetTree().Paused = false;
	}

	private void backToMenu()
	{
		GetTree().Paused = false;
		game.levelManager.clearArena();
		EmitSignal(SignalName.onSaveGame);
		SignalBus.bus.onNotifyPlayer -= showNotification;
		SignalBus.bus.onPlayerHealthBarUpdate -= playerHealthBarUpdate;
		SignalBus.bus.onLevelCompleted -= levelCompletedUpdate;
		SignalBus.bus.onWaveCompleted -= waveCompletedUpdate;
		SignalBus.bus.onCoinCollected -= coinCollected;
		SignalBus.bus.onBossReady -= setBossHealthbar;
		SignalBus.bus.onBossHit -= updateBossHealthbar;
		SignalBus.bus.onScrollCollected -= scrollCollected;
		SignalBus.bus.onGameOver -= gameOver;
		SignalBus.bus.onScrollUpdate -= updateActiveScroll;
		GetTree().ChangeSceneToPacked(AssetManager.instance.menuScene);
	}
}