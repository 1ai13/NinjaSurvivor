using Godot;
using System;

public partial class EnemyBoss : Enemy
{
	public AnimatedSprite2D bossAnimation;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		player = GetParent().GetNode<Player>("Player");
		bossAnimation = GetNode<AnimatedSprite2D>("EnemyArea/BossSprite");
		EntityHelper.initEnemy(this, data);
		bossAnimation.Play("idle");
		SignalBus.bus.onAwakeBoss += awakeBoss;
	}

	public void init(Vector2 pos)
	{

		Position = pos;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
	}

	protected override void performAttack()
	{
		throw new NotImplementedException();
	}

	private async void awakeBoss()
	{
		GD.Print("awaking boss");
		//Wake up boss
		bossAnimation.Play("awake");
		AssetManager.instance.playSFX("deadBamboo", -5f);
		AssetManager.instance.playSFX(GD.Load<AudioStream>("res://assets/audio/enemies/bosses/bamboo/bambooAwake.wav"), 10f);
		//Span for resetting camera
		await ToSignal(GetTree().CreateTimer(1), "timeout");
		bossAnimation.Play("idle");
		var tween = CreateTween();
		tween.TweenProperty(player.camera, "offset", Vector2.Zero, .5f);
		await ToSignal(tween, "finished");
		player.setPlayerProcess(true);
	}
}