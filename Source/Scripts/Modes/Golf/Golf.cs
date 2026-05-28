using Godot;
using System;
using System.Collections.Generic;

public partial class Golf : Mode, ILevelLoadedEvent{
    public static int TopStroke;
    public static sbyte Par;
	public static int FinishedPlayers;
    public static int[] PlayerStrokes;
    public static sbyte[] FinishedPlayerStrokes;
    private static sbyte[] sortedScores;
    private readonly Palette[] GOLF_PALETTES = new Palette[]{
        new Palette(new Color(0,1,201/255f),new Color(0,1,139/255f),new Color(0,189/255f,60/255f))
    };

    public override void _Ready() {
        LevelPalette = GOLF_PALETTES[Game.Random.Next(0, GOLF_PALETTES.Length)];
        Instructions = "Reach the hole in the least strokes";
        base._Ready();
        FinishedPlayers = 0;
        Game.CurrentMode = Mode.GameMode.Golf;
        Par = GetNode<Level>("Level").LevelUnit;
        FinishedPlayerStrokes = new sbyte[Game.MAX_PLAYERS];
        for (int i = 0; i < FinishedPlayerStrokes.Length; i++) FinishedPlayerStrokes[i] = sbyte.MaxValue;
        if (Game.TotalPlayers == 1) AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/Golf/GolfHud.tscn").Instantiate());
        //else foreach(Player player in Game.Players) player.Invulnerable = true; In player spawn function now in level class
        if (GolfCup.IsCup) GolfCup.HolePars.Add(Par);
        Scores = new float[Game.MAX_PLAYERS];
        isScoreMode = true;
        PlayerStrokes = new int[Game.TotalPlayers];
        TopStroke = int.MaxValue;
	}

    public void OnLevelLoaded(){
        if(Game.TotalPlayers != 1)
            foreach(Player player in Game.Players) player.Invulnerable = true;
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause){
        if(Death.DeathCause.Offscreen == deathCause) Rpc(nameof(NewStroke),player.Id);
    }

    public override void PlayerDisconnected(Player player){
        PlayerStrokes[player.Id-1] = 99;
		if(!player.Finished) PlayerInHole(player.Id);
    }

    public override void PlayerRespawned(Player player){
        base.PlayerRespawned(player);
        player.Visuals.ShowPlayerText();
        //if(Online.IsHost()) Rpc(nameof(GolfPlayerRespawned),player.Id);
    }

    public override void PlayerLaunched(Player player){
        base.PlayerLaunched(player);
        Rpc(nameof(NewStroke),player.Id);
    }

    public override void PlayerSlammed(Player player){
        base.PlayerSlammed(player);
        Rpc(nameof(NewStroke),player.Id);
    }

    //Golf
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public async void NewStroke(byte id){
        Player player = Game.Players[id-1];
        
		Golf.PlayerStrokes[player.Index]++;
		player.Visuals.ShowPlayerText();
		
		//Set Top Score
		//Create scores array
		int[] scores = new int[Game.TotalPlayers];
		for(int i = 0; i < scores.Length; i++) scores[i] = int.MaxValue;
		//Set each value for nonfinished players
		foreach(Player otherPlayer in Game.Players) scores[otherPlayer.Index] = Golf.PlayerStrokes[player.Index];
		//Set values of finished players
		for(int i = 0; i < Game.TotalPlayers; i++){
			if(scores[i] == int.MaxValue) scores[i] = Golf.FinishedPlayerStrokes[i];
		}

		//Golf.TopScore = scores.Min();

		Golf.TopStroke = scores[0];
		for(int i = 1; i < scores.Length; i++){
			if(scores[i] < Golf.TopStroke) Golf.TopStroke = scores[i];
		}

		int strokes = Golf.PlayerStrokes[player.Index];
		//Say hello to my little friend
		if(Game.TotalPlayers > 1){
			//Must account for Scores array starting filled by sbyte.MaxValue to get the realMax
			int max = 0;
			foreach(float score in Golf.FinishedPlayerStrokes){
				int intScore = (int)score;
				if(intScore != sbyte.MaxValue && intScore > max) max = intScore;
			}
			//You go boom if (you are ten times the par or over 99 strokes which ever is less) or you are guarenteed last place
			if((strokes >= Golf.Par * 10 && Golf.Par > 0) || strokes > 99 || (Golf.FinishedPlayers == Game.TotalPlayers - 1 && strokes > max)){
				Golf.ExplodePlayer(player);
			//Player is stalling on last stroke that guarenteed to die on so we start timer to KILL
			}else if( (strokes == (Golf.Par * 10)-1 && Golf.Par > 0) || strokes == 98 || (Golf.FinishedPlayers == Game.TotalPlayers - 1 && strokes == max)){
				await ToSignal(GetTree().CreateTimer(10,false), "timeout");
				if(!Finished){
					NewStroke(player.Id);
				}
			}
		}else if((strokes >= Golf.Par * 10 && Golf.Par > 0) || strokes > 99) Golf.ExplodePlayer(player);
	}

    public override string GetPlayerText(Player player){
        return "Stroke " + Golf.PlayerStrokes[player.Id-1];
    }

    public override void OnPlayerEnterRegain(Player player) {
        //Intentionally empty. Golf mode waits for the stillTimer.
    }

    public override void PlayerRegainCheck(Player player, float delta) {
        if(player.IsRegaining){
            if(player.StillTimer >= 2){
                player.CanLaunch = true;
                player.CanSlam = true;
                player.SetNewPos = false;
                player.StillTimer = 0;
            }else if(player.StillTimer >= 1 && player.StillTimer < 2 && Mathf.Abs(player.Rb.GlobalPosition.DistanceTo(player.SpawnPoint)) < 512 && Golf.PlayerStrokes[player.Index] != 0){
                player.PlayerEmotion = Player.Emotion.Annoyed;
            }
            player.StillTimer += delta;
        }

        if(player.Invulnerable && Golf.PlayerStrokes[player.Index] == 0 && Game.TotalPlayers != 1){
            player.InvulnerabilityTimer = 0.5f;
        }

        if(!player.IsRegaining && !player.SetNewPos && !Level.IsPositionOffscreenOrDead(player.Rb.GlobalPosition)){
            player.SetNewPos = true;
            player.SpawnPoint = player.Rb.GlobalPosition;
        }
    }
    public override float MoonAirTimeRequirement => 2.0f;
    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void GolfPlayerRespawned(byte playerId){
        Player player = Game.Players[playerId-1];
        player.Visuals.ShowPlayerText();
        player.PlayerEmotion = Player.Emotion.Annoyed;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public async void PlayerInHole(byte id){
        Player player = Game.Players[id-1];
        if(!player.Finished){
            player.Finished = true;
            FinishedPlayerStrokes[player.Id - 1] = (sbyte)PlayerStrokes[player.Id-1];//player.Score;
            FinishedPlayers++;
            SFX.Play("GolfHole");
            if(Online.IsHost()){
                if(FinishedPlayers >= Game.TotalPlayers){
                    if(!GolfCup.IsCup) GameFinished();
                }else if(FinishedPlayers == Game.TotalPlayers -1){
                    int max = 0;
                    Player nonFinishedPlayer = null;
                    for(int i = 0; i < Game.TotalPlayers; i++){
                        if(!Game.Players[i].Finished) nonFinishedPlayer = Game.Players[i];
                        int intScore = FinishedPlayerStrokes[i];
		    		    if(intScore != sbyte.MaxValue && intScore > max) max = intScore;
                    }
                    //Start timer to KILL STALLING PLAYER
                    if(nonFinishedPlayer != null){
                        if(PlayerStrokes[nonFinishedPlayer.Id-1] >= max){
                            await ToSignal(GetTree().CreateTimer(10,false), "timeout");
		    		        if(!nonFinishedPlayer.Finished){
                                Rpc(nameof(NewStroke),nonFinishedPlayer.Id);
                                //nonFinishedPlayer.NewStroke();
                            }
                        }
                    }
                }
            }
        }
    }

    protected override void SetPoints(){
        sortedScores = (sbyte[])FinishedPlayerStrokes.Clone();
        Array.Sort(sortedScores);  // Sort the strokes to determine positions

        for(int i = 0; i < Game.TotalPlayers; i++){
            Positions[i] = (byte)(Array.IndexOf(sortedScores, FinishedPlayerStrokes[i]) + 1);
        }

        // Copy Strokes to Scores (if that's the intended behavior)
        for(int i = 0; i < FinishedPlayerStrokes.Length; i++) Scores[i] = FinishedPlayerStrokes[i];
    }

    public override Item GiveItem(Player player){
        float deficit;
        if(FinishedPlayers == 0){
            deficit = PlayerStrokes[player.Id-1] / (Par*1.25f);
        }else{
            deficit = PlayerStrokes[player.Id-1] / MathF.Pow(TopStroke,-0.75f) + 2;
        }

        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new BowlingBall(player), 9),
            Tuple.Create((Item)new Inverter(player), 8),
            Tuple.Create((Item)new Moon(player), 8),
            Tuple.Create((Item)new Wings(player), 7),
            Tuple.Create((Item)new SmallBall(player), 7),
            Tuple.Create((Item)new Pepper(player,2), 6),
            Tuple.Create((Item)new StopSign(player,2), 5),
            Tuple.Create((Item)new Booll(player), 4),
            Tuple.Create((Item)new BigFungus(player), 2),
            Tuple.Create((Item)new Ball(player,2), 1)
        };
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}

    public static void ExplodePlayer(Player player){
        ParticleManager.ParticleManagerNode.SpawnExplosion(player.Rb.GlobalPosition,0.5f);
		player.Visible = false;
		if(Online.IsHost()) (Mode.ModeNode as Golf).PlayerInHole(player.Id);
    }
}