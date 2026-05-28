using Godot;

public partial class RaceCheckpoint : Area2D{
	[Export] 
	public int id;
	[Export]
	public bool isFinish = false;
    public static int TotalCheckpoints;
    public static int FinishedPlayers = 0;
	private AudioStreamPlayer2D lapSound, finishSound;

    public override void _Ready(){
        FinishedPlayers = 0;
		if(isFinish)ZIndex = -1;	
    }

	public void _body_entered(PhysicsBody2D body) {
    	if(body.GetParent().IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			if(!Online.IsOnline || Online.IsHost()){
				CheckPointEntered(player.Id,Race.RaceTimer);
			}
    	}
	}

	//Runs on Host
	private void CheckPointEntered(byte playerId,float playersTime){
		Player player = Game.Players[playerId-1];
		int playerLap = Race.PlayerLaps[player.Id-1];
		//Player crosses checkpoint
        if(id == Race.PlayerCheckpoints[player.Id-1] + 1 && !isFinish){
			Race.PlayerCheckpoints[player.Id-1]++;
			if(playerLap >= Race.TopLap && Race.PlayerCheckpoints[player.Id-1] >= Race.TopCheckPoint){
				Race.TopLap = playerLap;
				Race.TopCheckPoint = Race.PlayerCheckpoints[player.Id-1];
			}
		}
		//Player crosses Finish Line
		if(isFinish){
			//Lap Completed
			if(Race.PlayerCheckpoints[player.Id-1] == TotalCheckpoints - 1){
				Race.PlayerLaps[player.Id-1]++; //Can't use playerLap because we need to increase the actual variable (pointer)
				playerLap = Race.PlayerLaps[player.Id-1]; //Sync for readability
            	Race.PlayerCheckpoints[player.Id-1] = 0;
            	//Player finishes
            	if(playerLap >= Race.TotalLaps){
					SFX.Play("Finish");
            	    player.Finished = true;
            	    Race.PlayerFinished(player,playersTime);
				}else SFX.Play("Lap");
				if(playerLap > Race.TopLap){
					Race.TopLap = playerLap;
					Race.TopCheckPoint = 0;
				}
				Rpc(nameof(ClientCrossedFinishLine),player.Id,(byte)playerLap,true);
			}else{ //Sets checkpoint back to finish line if not completed lap
				Race.PlayerCheckpoints[player.Id-1] = 0;
				Rpc(nameof(ClientCrossedFinishLine),player.Id,(byte)playerLap,false);
			}
		
		//Sets checkpoint back if gone back
		}else if(id < Race.PlayerCheckpoints[player.Id-1]){
			Race.PlayerCheckpoints[player.Id-1] = id;
			player.PlayerEmotion = Player.Emotion.Angry;
		}
        //If Multiplayer finished
		if(FinishedPlayers >= Game.TotalPlayers -1 && !Mode.Finished && Game.TotalPlayers > 1) Mode.GameFinished();
		//If Solo finished
		else if(Game.TotalPlayers == 1 && Race.PlayerLaps[player.Id-1] >= Race.TotalLaps) Mode.GameFinished();
	}

	//Host sends this Rpc to clients to update the player's HUD and play sfx
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ClientCrossedFinishLine(byte playerId,byte lap,bool lapCompleted){
		Player player = Game.Players[playerId-1];
		if(!Online.IsHost()) Race.PlayerLaps[player.Id-1] = lap;
		player.Visuals.ShowPlayerText();
		if(lapCompleted){
			if(lap >= Race.TotalLaps){
				SFX.Play("Finish");
				player.Finished = true;
				foreach(Player otherPlayer in Game.Players){
					for(int i = 0; i < Game.Players.Length; i++){
						if(!Game.Players[i].Finished && Race.PlayerLaps[i] < Race.TotalLaps-1){
							MusicPlayer.SetPitch(1);
							break;
						}
					}
				}
			}else{
				SFX.Play("Lap");
				const float FINAL_LAP_PITCH = 1.25f;
				if(lap == Race.TotalLaps-1 && MusicPlayer.GetPitch() != FINAL_LAP_PITCH){
					MusicPlayer.SetPitch(FINAL_LAP_PITCH);
				}
			}
		}
	}
}