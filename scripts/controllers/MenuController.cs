using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class MenuController : Control
{
	[Export]
	private Character[] characters;
	private int charSelected;
	private Array<int> charsCollected;
	private RichTextLabel goldCounter;
	private int playerGold;
	private TextureRect selectedCharIcon;
	private TextureRect meleeTexture;
	private TextureRect rangedTexture;
	private Label characterName;
	private Label healthStat;
	private Label meleeDamage;
	private Label rangedDamage;
	private Label speedStat;
	private TextureButton leftChar;
	private TextureButton rightChar;
	private Button buyButton;
	private Button startButton;
	private Button controlsButton;
	private Panel controlsInfo;
	private ConfirmationDialog quitModal;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		goldCounter = GetNode<RichTextLabel>("GoldCounter/GoldCounter");
		selectedCharIcon = GetNode<TextureRect>("SelectedCharacter");
		meleeTexture = GetNode<TextureRect>("CharacterStats/MeleeWeapon");
		rangedTexture = GetNode<TextureRect>("CharacterStats/RangedWeapon");
		characterName = GetNode<Label>("CharacterName");
		healthStat = GetNode<Label>("CharacterStats/HealthStat");
		meleeDamage = GetNode<Label>("CharacterStats/MeleeDamage");
		rangedDamage = GetNode<Label>("CharacterStats/RangedDamage");
		speedStat = GetNode<Label>("CharacterStats/SpeedStat");
		leftChar = GetNode<TextureButton>("LeftChar");
		rightChar = GetNode<TextureButton>("RightChar");
		buyButton = GetNode<Button>("BuyButton");
		startButton = GetNode<Button>("StartButton");
		controlsButton = GetNode<Button>("Controls");
		controlsInfo = GetNode<Panel>("ControlsInfo");
		quitModal = GetNode<ConfirmationDialog>("QuitConfirm");
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err == Error.FileNotFound)
		{
			statsFile.SetValue("player", "gold", 0);
			statsFile.SetValue("player", "scrolls", 0);
			statsFile.SetValue("player", "activeScroll", 0);
			statsFile.SetValue("game", "selectedCharacter", 0);
			statsFile.SetValue("game", "charactersCollected", new Array<int> { 0 });
			statsFile.SetValue("game", "soundFXVolume", 1);
			statsFile.Save("user://stats.cfg");
		}
		err = statsFile.Load("user://stats.cfg");
		if (err == Error.Ok)
		{
			charSelected = (int)statsFile.GetValue("game", "selectedCharacter");
			charsCollected = (Array<int>)statsFile.GetValue("game", "charactersCollected");
			playerGold = (int)statsFile.GetValue("player", "gold");
		}
		if (charSelected == 0)
		{
			leftChar.Hide();
		}
		else if (charSelected == characters.Length - 1)
		{
			rightChar.Hide();
		}
		goldCounter.Text = $"{playerGold}[font_size=8]  g[/font_size]";
		switchStats(charSelected);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Escape))
		{
			if (controlsInfo.Visible)
			{
				controlsInfo.Hide();
			}
		}
	}

	public void switchStats(int charSelected)
	{
		if (!charsCollected.Contains(charSelected))
		{
			buyButton.Show();
			buyButton.Text = "" + charSelected * 50;
			startButton.Modulate = Color.Color8(255, 255, 255, 170);
			startButton.Disabled = true;
		}
		else
		{
			buyButton.Hide();
			startButton.Modulate = Colors.White;
			startButton.Disabled = false;
		}
		Character c = characters[charSelected]; ;
		selectedCharIcon.Texture = c.icon;
		characterName.Text = c.name;
		healthStat.Text = "" + c.health / 100;
		meleeTexture.Texture = c.meleeWeapon.icon;
		meleeDamage.Text = "" + c.meleeWeapon.damage;
		rangedTexture.Texture = c.rangedWeapon.icon;
		rangedDamage.Text = "" + c.rangedWeapon.damage;
		speedStat.Text = "" + (int)c.speed / 10;
	}
	private void switchLeftCharacter()
	{
		charSelected = Math.Max(0, charSelected - 1);
		if (!rightChar.Visible) rightChar.Show();
		if (charSelected == 0)
		{
			leftChar.Hide();
		}
		switchStats(charSelected);
		saveCharacterSelected();
	}
	private void switchRightCharacter()
	{
		charSelected = Math.Max(0, charSelected + 1);
		if (!leftChar.Visible) leftChar.Show();
		if (charSelected == characters.Length - 1)
		{
			rightChar.Hide();
		}
		switchStats(charSelected);
		saveCharacterSelected();
	}
	private void saveCharacterSelected()
	{
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err != Error.Ok || !charsCollected.Contains(charSelected))
		{
			return;
		}
		statsFile.SetValue("game", "selectedCharacter", charSelected);
		statsFile.Save("user://stats.cfg");
	}
	private void saveStats()
	{
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err != Error.Ok)
		{
			return;
		}
		statsFile.SetValue("game", "selectedCharacter", charSelected);
		statsFile.SetValue("game", "charactersCollected", charsCollected);
		statsFile.SetValue("player", "gold", playerGold);
		statsFile.Save("user://stats.cfg");
	}
	private void onBuyCharacter()
	{
		string gold = buyButton.Text.Substr(0, goldCounter.Text.IndexOf("["));
		int buyGold = int.Parse(gold);
		if (playerGold >= buyGold)
		{
			playerGold -= buyGold;
			goldCounter.Text = goldCounter.Text = $"{playerGold}[font_size=8]  g[/font_size]";
			charsCollected.Add(charSelected);
			buyButton.Hide();
			AssetManager.instance.playSFX(GD.Load<AudioStream>("res://assets/audio/ui/buyCharacter.wav"));
			saveStats();
		}
	}

	private void controlsPressed()
	{
		controlsInfo.Show();
	}

	private void quitPressed()
	{
		quitModal.Show();
	}

	private void startPressed()
	{
		AssetManager.instance.charSelected = characters[charSelected];
		Error err = GetTree().ChangeSceneToPacked(AssetManager.instance.gameScene);
	}

	private void mouseHoveringButton()
	{
		AssetManager.instance.playSFX("buttonHover");
	}

	private void exitGame()
	{
		GetTree().Quit();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			GetTree().AutoAcceptQuit = false;
			quitModal.Show();
		}
	}
}
