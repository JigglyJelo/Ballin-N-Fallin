using Godot;
using System.Collections.Generic;

public partial class Death : Node{
	public static Death DeathNode;
	public static Dictionary<int,float> RespawnTimes;
	private List<int> respawnTimesToDelete;
	private readonly PackedScene RESPAWN_POINT_INDICATOR = GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Players/RespawnPointIndicator.tscn");

	public override void _Ready(){
		DeathNode = this;
		RespawnTimes = new Dictionary<int, float>();
		respawnTimesToDelete = new List<int>();
		SetProcess(false);
	}

	public override void _PhysicsProcess(double delta){
		float fDelta = (float)delta;

		foreach(KeyValuePair<int,float> keyValuePair in RespawnTimes){
			int id = keyValuePair.Key;
			RespawnTimes[id] -= fDelta;
			if(RespawnTimes[id] <= 0){
				respawnTimesToDelete.Add(id);
			}
			//If i dont do this then when player respawns there is a burst of velocity in random direction so thanks godot physics
			Game.Players[id-1].Rb.LinearVelocity = Vector2.Zero;
			Game.Players[id-1].Rb.GlobalPosition = new Vector2(100000,-100000);
		}

		if(respawnTimesToDelete.Count > 0){
			foreach(int id in respawnTimesToDelete){
				RespawnTimes.Remove(id);
				if(Online.IsHost()) Rpc(nameof(CreateRespawnPointIndicator),Game.Players[id-1].SpawnPoint,id);
			}
			respawnTimesToDelete = new List<int>();
		}
	}

	public static void KillPlayer(Player player, DeathCause deathCause){
		if(Online.IsHost()){
			float respawnTime = 2;
			switch(deathCause){
				case DeathCause.Offscreen:
					player.Rpc(nameof(player.SpawnBlastParticles),player.Rb.GlobalPosition);
					break;
				case DeathCause.Pop:
					player.Rpc(nameof(player.SpawnPopParticles));
					break;
				case DeathCause.Stomp:
					player.Rpc(nameof(player.SpawnPopParticles));
					break;
				case DeathCause.Respawn:
					respawnTime = 0.1f;
					break;
			}

			Mode.ModeNode.PlayerDied(player,deathCause); //Set player's respawn point and other mode specific stuff that occurs when you die
			//player.Rb.SetDeferred("global_position",new Vector2(100000+(Game.Random.NextSingle()*10000),-100000-(Game.Random.NextSingle()*10000)));
			player.Rb.SetDeferred("global_position",new Vector2(100000,-100000));
        	player.Rb.SetDeferred("linear_velocity",Vector2.Zero);
			player.Rb.SetDeferred("angular_velocity",0);
        	//player.Smoother.CallDeferred("teleport");
			player.Rb.SkipInterpolation();
			player.Trail.Rpc(nameof(player.Trail.ResetTrail));
			player.RpcId(player.OwnerId,nameof(player.PlayerOffscreenOnHost));
    		player.Rb.SetDeferred("freeze",true);
			
			if(!player.Finished && !RespawnTimes.ContainsKey(player.Id)) RespawnTimes.Add(player.Id,respawnTime);
		}else{
			player.TicksToIgnore = PingGetter.PingToTicks(PingGetter.GetMedianPing())+1;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveLife(byte id){
        switch(Game.CurrentMode){
            case Mode.GameMode.Deathmatch:
				Deathmatch.PlayerLives[id-1]--;
				break;
            case Mode.GameMode.Survival:
                Survival.PlayerLives[id-1]--;
                break;
        }
    }

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)] //Give own channel
	private void CreateRespawnPointIndicator(Vector2 position, byte playerId){
		RespawnPointIndicator respawnPointIndicator = RESPAWN_POINT_INDICATOR.Instantiate<RespawnPointIndicator>();
		respawnPointIndicator.Player = Game.Players[playerId-1];
		AddChild(respawnPointIndicator);
		respawnPointIndicator.GlobalPosition = position;
	}

	public enum DeathCause{
		Offscreen, // Causes the blast particles to appear
		Pop, // Causes the Pop particles to appear
		Stomp, // Also causes pop particles
		Respawn // No effect
	}
}