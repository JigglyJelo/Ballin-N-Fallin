using System;
using Godot;

public partial class ParticleManager : Node{
	public static ParticleManager ParticleManagerNode;
	private int bounceIndex = 0;
	private BounceParticles[] bounceParticles;
	private int launchIndex = 0;
	private CpuParticles2D[] launchParticles, popParticles;
	private CpuParticles2D explosionParticles;
	private int popIndex = 0;
	
	public override void _Ready(){
		ParticleManagerNode = this;
		int playerParticleCount = (Game.TotalPlayers * 2) + (Game.TotalPlayers/4) + (Game.TotalPlayers == 1 ? 1 : 0);
		bounceIndex = 0;
		bounceParticles = new BounceParticles[playerParticleCount];
		PackedScene bounceParticleScene = GD.Load<PackedScene>("res://Scenes/Object Scenes/Particles/BounceParticles.tscn");
		launchIndex = 0;
		launchParticles = new CpuParticles2D[playerParticleCount];
		PackedScene launchParticleScene = GD.Load<PackedScene>("res://Scenes/Object Scenes/Particles/LaunchParticles.tscn");
		for(int i = 0; i < playerParticleCount; i++){
			bounceParticles[i] = bounceParticleScene.Instantiate<BounceParticles>();
			AddChild(bounceParticles[i]);
			launchParticles[i] = launchParticleScene.Instantiate<CpuParticles2D>();
			AddChild(launchParticles[i]);
		}
		PackedScene popParticleScene = GD.Load<PackedScene>("res://Scenes/Object Scenes/Particles/Pop Particles.tscn");
		int popParticleCount = (int)MathF.Ceiling(Game.TotalPlayers / 2f);
		popParticles = new CpuParticles2D[popParticleCount];
		for(int i = 0; i < popParticleCount; i++){
			popParticles[i] = popParticleScene.Instantiate<CpuParticles2D>();
			AddChild(popParticles[i]);
		}
		popIndex = 0;
		if(Game.CurrentMode == Mode.GameMode.Golf || Game.CurrentMode == Mode.GameMode.HotPotato || Game.CurrentMode == Mode.GameMode.BombBall){
			explosionParticles = GD.Load<PackedScene>("res://Scenes/Object Scenes/Particles/Explosion.tscn").Instantiate<CpuParticles2D>();
			AddChild(explosionParticles);
		}
		SetProcess(false);
		SetPhysicsProcess(false);
	}

	public static void SpawnBounceParticle(Vector2 position,Vector2 velocity,Player player,int preProcessTicks){
		BounceParticles bounceParticle = ParticleManagerNode.bounceParticles[ParticleManagerNode.bounceIndex];
		if(++ParticleManagerNode.bounceIndex == ParticleManagerNode.bounceParticles.Length) ParticleManagerNode.bounceIndex = 0;
		bounceParticle.OneShot = false;
		bounceParticle.DustParticles.OneShot = false;
		bounceParticle.Emitting = false;
		bounceParticle.DustParticles.Emitting = false;
		bounceParticle.GlobalPosition = position - (velocity.Normalized() * (75 * player.PlayerScale));
		bounceParticle.Preprocess = (float)preProcessTicks/Engine.PhysicsTicksPerSecond;
		bounceParticle.SelfModulate = player.PlayerColor;
		bounceParticle.Scale = new Vector2(player.PlayerScale,player.PlayerScale);
		bounceParticle.StartEmitting(velocity);
	}

	public static void SpawnLaunchParticles(float angle, float magnitude,Vector2 position,float preprocess){
		CpuParticles2D launchParticle = ParticleManagerNode.launchParticles[ParticleManagerNode.launchIndex];
		if(++ParticleManagerNode.launchIndex == ParticleManagerNode.launchParticles.Length) ParticleManagerNode.launchIndex = 0;
		const float PREPROCESS = 0.15f;
		const float LIFETIME = 0.2f;
		const float MIN_VEL = 200;
		const float MAX_VEL = 500;
		if(launchParticle.Emitting) launchParticle.Emitting = false;
		float velMultiplier = 1 + (magnitude/Player.MAX_LAUNCH_POWER);
		launchParticle.GlobalPosition = position;
		launchParticle.Rotation = angle;
		launchParticle.InitialVelocityMin = MIN_VEL * velMultiplier;
		launchParticle.InitialVelocityMax = MAX_VEL * velMultiplier;
		float preprocessAmount = PREPROCESS * velMultiplier;
		launchParticle.Lifetime = LIFETIME + preprocessAmount;
		launchParticle.Preprocess = preprocessAmount + preprocess;
		launchParticle.Emitting = true;
	}

	public static void SpawnPopParticles(Vector2 position, Color color){
		CpuParticles2D popParticle = ParticleManagerNode.popParticles[ParticleManagerNode.popIndex];
		if(++ParticleManagerNode.popIndex == ParticleManagerNode.popParticles.Length) ParticleManagerNode.popIndex = 0;
		popParticle.SelfModulate = color;
		if(popParticle.Emitting) popParticle.Emitting = false;
		popParticle.GlobalPosition = position;
		popParticle.Emitting = true;
		SFX.Play("Pop",position);
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.DeathParticle)]
	public void SpawnExplosion(Vector2 position){
		SpawnExplosion(position, 1);
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.DeathParticle)]
	public void SpawnExplosion(Vector2 position, float scale){
		if(ParticleManagerNode.explosionParticles != null){
			ParticleManagerNode.explosionParticles.Scale = new Vector2(scale, scale);
			ParticleManagerNode.explosionParticles.GlobalPosition = position;
			ParticleManagerNode.explosionParticles.Emitting = true;
			SFX.Play("Explosion",position);
		}else{
			GD.PrintErr("Tried to spawn explosion particles in mode without them loaded");
		}
	}
}