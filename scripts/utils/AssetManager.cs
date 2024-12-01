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
	private RandomNumberGenerator rnd;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Initialize the private Singleton Autoload
		instance = this;

		projectileScene = GD.Load<PackedScene>("res://scenes/Projectile.tscn");
		rnd = new RandomNumberGenerator();

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

		//Playing it without AudioStream for PolyphonicPlayback assignment not null
		SFXPlayer.Play();
	}

	private void addSFX(string name, string path)
	{
		var sfx = GD.Load<AudioStream>(path);
		if (!SFXAudios.ContainsKey(name))
		{
			SFXAudios[name] = new List<AudioStream> { sfx };
		}
		else
		{
			SFXAudios[name].Add(sfx);
		}
	}

	private void addMusic(string name, string path)
	{
		var music = GD.Load<AudioStream>(path);
		if (!musicAudios.ContainsKey(name))
		{
			musicAudios[name] = new List<AudioStream> { music };
		}
		else
		{
			musicAudios[name].Add(music);
		}
	}

	public void playSFX(string name, float volume = 0)
	{
		AudioStream sfx;
		if (SFXAudios.ContainsKey(name))
		{
			sfx = name switch
			{
				"meleeAttack" => SFXAudios[name][rnd.RandiRange(0, SFXAudios[name].Count - 1)],
				_ => SFXAudios[name][0]
			};
			var playback = (AudioStreamPlaybackPolyphonic)SFXPlayer.GetStreamPlayback();
			playback.PlayStream(sfx, 0, volume, 1, 0, "SFX");
		}
	}

}