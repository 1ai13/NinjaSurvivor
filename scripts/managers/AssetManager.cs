using Godot;
using System;
using System.Collections.Generic;
public partial class AssetManager : Node
{
	public static AssetManager instance { get; private set; }
	private Dictionary<string, List<AudioStream>> SFXAudios;
	private Dictionary<string, List<AudioStream>> musicAudios;
	private AudioStreamPlayer2D SFXPlayer;
	private AudioStreamPlayer musicPlayer;
	public PackedScene projectileScene;
	public PackedScene itemScene;
	public PackedScene enemyHealthBarLabel;
	public PackedScene playerHealthBarScene;
	public PackedScene buffStatCounter;
	public PackedScene buffRandomBuff;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Initialize the private Singleton Autoload
		instance = this;

		//Load re-usable scenes
		projectileScene = GD.Load<PackedScene>("res://scenes/objects/Projectile.tscn");
		itemScene = GD.Load<PackedScene>("res://scenes/objects/Item.tscn");
		enemyHealthBarLabel = GD.Load<PackedScene>("res://scenes/ui/HealthBarLabel.tscn");
		playerHealthBarScene = GD.Load<PackedScene>("res://scenes/ui/PlayerHealthBar.tscn");
		buffStatCounter = GD.Load<PackedScene>("res://scenes/ui/BuffStatCounter.tscn");
		buffRandomBuff = GD.Load<PackedScene>("res://scenes/ui/ModalRandomBuff.tscn");

		//Audio Manager
		SFXAudios = new Dictionary<string, List<AudioStream>>();
		musicAudios = new Dictionary<string, List<AudioStream>>();
		SFXPlayer = new AudioStreamPlayer2D();
		SFXPlayer.Bus = "SFX";
		//Max number of SFX's in parallel
		SFXPlayer.MaxPolyphony = 20;
		//Needed for playing multiple sounds in parallel, PlaybackPolyphonic assigned to  AudioPlayer's Stream
		SFXPlayer.Stream = new AudioStreamPolyphonic();
		musicPlayer = new AudioStreamPlayer();
		musicPlayer.Bus = "Music";
		AddChild(SFXPlayer);
		AddChild(musicPlayer);
		//Adding SFX
		addSFX("meleeAttack", "res://assets/audio/player/meleeAttack.wav");
		addSFX("meleeAttack", "res://assets/audio/player/meleeAttack2.wav");
		addSFX("meleeAttack", "res://assets/audio/player/meleeAttack3.wav");
		addSFX("rangedAttack", "res://assets/audio/player/rangedAttack.wav");
		addSFX("rangedWallHit", "res://assets/audio/player/rangedWallHit.wav");
		addSFX("playerHit", "res://assets/audio/player/hit.wav");
		addSFX("enemyHit", "res://assets/audio/enemies/hit.wav");
		addSFX("batAttack", "res://assets/audio/enemies/bat/batAttack.wav");
		addSFX("batAttackHit", "res://assets/audio/enemies/bat/batAttackHit.wav");
		addSFX("deadBat", "res://assets/audio/enemies/bat/deadBat.wav");
		addSFX("slimeAttack", "res://assets/audio/enemies/slime/slimeAttack.wav");
		addSFX("deadSlime", "res://assets/audio/enemies/slime/deadSlime.wav");
		addSFX("enemySpawn", "res://assets/audio/map/spawnVortex.wav");
		addSFX("openDoor", "res://assets/audio/map/doorOpen.wav");
		addSFX("doorTp", "res://assets/audio/map/doorTp.wav");
		addSFX("levelCompleted", "res://assets/audio/map/levelCompleted.wav");
		addSFX("bambooAttackHit", "res://assets/audio/enemies/bamboo/bambooAttackHit.wav");
		addSFX("deadBamboo", "res://assets/audio/enemies/bamboo/deadBamboo.wav");
		addSFX("bambooAttack", "res://assets/audio/enemies/bamboo/bambooAttack.wav");
		addSFX("coinCollected", "res://assets/audio/items/coinCollected.wav");
		addSFX("heartHeal", "res://assets/audio/items/heartHeal.wav");
		addSFX("itemDrop", "res://assets/audio/items/itemDrop.wav");
		addSFX("buttonHover", "res://assets/audio/ui/buttonHover.wav");
		addSFX("openBuffer", "res://assets/audio/ui/openBuffer.wav");
		addSFX("closeBuffer", "res://assets/audio/ui/closeBuffer.wav");

		//Playing it without AudioStream for PolyphonicPlayback assignment not null
		SFXPlayer.Play();
	}

	private void addSFX(string name, string path)
	{
		loadSound(SFXAudios, name, path);
	}

	private void addMusic(string name, string path)
	{
		loadSound(musicAudios, name, path);

	}

	public void playSFX(string name, float volume = 0)
	{
		AudioStream sfx;
		if (SFXAudios.ContainsKey(name))
		{
			sfx = name switch
			{
				//If melee attack choose a random sound
				"meleeAttack" => SFXAudios[name][EntityHelper.rnd.RandiRange(0, SFXAudios[name].Count - 1)],
				_ => SFXAudios[name][0]
			};
			var playback = (AudioStreamPlaybackPolyphonic)SFXPlayer.GetStreamPlayback();
			playback.PlayStream(sfx, 0, volume, 1, 0, "SFX");
		}
	}


	public void playSFX(AudioStream sfx, float volume = 0)
	{
		var playback = (AudioStreamPlaybackPolyphonic)SFXPlayer.GetStreamPlayback();
		playback.PlayStream(sfx, 0, volume, 1, 0, "SFX");
	}

	private void loadSound(Dictionary<string, List<AudioStream>> map, string name, string path)
	{
		var sound = GD.Load<AudioStream>(path);
		if (!map.ContainsKey(name))
		{
			map[name] = new List<AudioStream> { sound };
		}
		else
		{
			map[name].Add(sound);
		}
	}
}
