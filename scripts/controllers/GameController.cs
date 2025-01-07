using Enums;
using Godot;
using Godot.Collections;
using System;


public partial class GameController : Node2D
{
	public Player player;
	public Marker2D playerSpawn;
	public Marker2D bossSpawn;
	[Export]
	private string characterSelected;
	public int level = 0;
	[Export]
	public Array<BiomeConfig> biomeConfigs;
	public Array<Rect2> trapsPosition;
	public BufferController buffer;
	Vector2I playerFeetOffset = new Vector2I(0, 7);
	public bool areTrapsActive = false;
	public ConfigFile stats;
	private HudController hud;
	public TileMapLayer arena;
	public LevelManager levelManager;
	private ConfirmationDialog quitModal;
	public override void _Ready()
	{
		player = GetNode<Player>("Player");
		arena = GetNode<TileMapLayer>("Arena");
		playerSpawn = GetNode<Marker2D>("Arena/PlayerRespawn");
		bossSpawn = GetNode<Marker2D>("Arena/BossSpawn");
		buffer = GetNode<BufferController>("Buffer");
		hud = GetNode<HudController>("CanvasLayer/HUD");
		trapsPosition = new Array<Rect2>();
		quitModal = GetNode<ConfirmationDialog>("QuitConfirm");
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err == Error.Ok)
		{
			player.gold = (int)statsFile.GetValue("player", "gold");
			player.scrollsCollected = (int)statsFile.GetValue("player", "scrolls");
			player.activeScroll = (int)statsFile.GetValue("player", "activeScroll");
			hud.soundFXSlider.Value = (float)statsFile.GetValue("game", "soundFXVolume");
			hud.pausedFXSlider.Value = (float)statsFile.GetValue("game", "soundFXVolume");
			hud.lastSoundFXVolume = (float)hud.soundFXSlider.Value;
			AudioServer.SetBusVolumeDb(hud.SFXBusIndex, (float)Mathf.LinearToDb(hud.soundFXSlider.Value));
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onCoinCollected));
			SignalBus.bus.EmitSignal(nameof(SignalBus.bus.onScrollUpdate));
		}
		player.loadCharacter(AssetManager.instance.charSelected, playerSpawn.Position);
		levelManager = new LevelManager(this, arena);
		// //TODO Add more biomes & enemies
	}

	public override void _Process(double delta)
	{
		//Hurt player in case standing on Traps
		if (areTrapsActive)
		{
			foreach (var t in trapsPosition)
			{
				if (t.HasPoint(player.GlobalPosition + playerFeetOffset))
				{
					areTrapsActive = false;
					player.takeDamage((int)(player.maxHealth * .25f));
				}
			}
		}
	}

	private void saveGame()
	{
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err != Error.Ok)
		{
			return;
		}
		statsFile.SetValue("player", "gold", player.gold);
		statsFile.SetValue("player", "scrolls", player.scrollsCollected);
		statsFile.SetValue("player", "activeScroll", player.activeScroll);
		statsFile.SetValue("game", "soundFXVolume", hud.pausedFXSlider.Value);
		statsFile.Save("user://stats.cfg");
	}
	private void saveConfig()
	{
		var statsFile = new ConfigFile();
		Error err = statsFile.Load("user://stats.cfg");
		if (err != Error.Ok)
		{
			return;
		}
		statsFile.SetValue("game", "soundFXVolume", hud.pausedFXSlider.Value);
		statsFile.Save("user://stats.cfg");
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
