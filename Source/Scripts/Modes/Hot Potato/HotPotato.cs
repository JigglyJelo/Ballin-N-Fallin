using Godot;
using System;
using System.Collections.Generic;

public partial class HotPotato : Mode, ILevelLoadedEvent{
	private static byte deadPlayers;
    private static float bombTimer;
    private const float STARTING_BOMB_PITCH = 0.5f;
    public static float BombPitch = STARTING_BOMB_PITCH;
    public float[] TagTimers = new float[Game.TotalPlayers];
    private readonly Palette[] HOT_POTATO_PALETTES = new Palette[]{
        new Palette(new Color(0.5f,0.5f,0.5f),new Color(0.25f,0.25f,0.25f),new Color(0.0625f,0.0625f,0.0625f))
    };
    private Vector2 deathPoint;
    private Player potatoHolder = null;
    public static Player PotatoHolder{
        get{return (Mode.ModeNode as HotPotato).potatoHolder;}
		set{
            HotPotato modeNode = Mode.ModeNode as HotPotato;
            //Remove bomb from old potato holder
            if(modeNode.potatoHolder != null){
                modeNode.potatoHolder.Visuals.BallSprite.SelfModulate = new Color(modeNode.potatoHolder.PlayerColor,modeNode.potatoHolder.Visuals.BallSprite.SelfModulate.A);
                modeNode.potatoHolder.Visuals.LinesSprite.SelfModulate = new Color(Colors.Black,modeNode.potatoHolder.Visuals.LinesSprite.SelfModulate.A);
                modeNode.potatoHolder.Visuals.ShadingSprite.SelfModulate = new Color(modeNode.potatoHolder.PlayerColor,modeNode.potatoHolder.Visuals.ShadingSprite.SelfModulate.A);
				modeNode.potatoHolder.PlayerEmotion = Player.Emotion.Neutral;
            }
            modeNode.potatoHolder = value;
            if(value != null){
                modeNode.potatoHolder.Visuals.BallSprite.SelfModulate = new Color(Color.Color8(15,15,15),modeNode.potatoHolder.Visuals.BallSprite.SelfModulate.A);
                modeNode.potatoHolder.Visuals.LinesSprite.SelfModulate = new Color(modeNode.potatoHolder.PlayerColor,modeNode.potatoHolder.Visuals.LinesSprite.SelfModulate.A);
                modeNode.potatoHolder.Visuals.ShadingSprite.SelfModulate = new Color(Colors.Black,modeNode.potatoHolder.Visuals.ShadingSprite.SelfModulate.A);
				modeNode.potatoHolder.PlayerEmotion = Player.Emotion.Shocked;
            }
        }
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready(){
        LevelPalette = HOT_POTATO_PALETTES[Game.Random.Next(0,HOT_POTATO_PALETTES.Length)];
        Instructions = "Pass the bomb before the boom";
        base._Ready();
        deadPlayers = 0;
        Game.CurrentMode = Mode.GameMode.HotPotato;
        //Initial Bomb is given in the Spawn Player Function in the Level Class now
        isScoreMode = false;
	}

	public override void _PhysicsProcess(double delta){
        if(Online.IsHost()){
            float fDelta = (float)delta;
            bombTimer -= fDelta;
            if(bombTimer <= 0){
                foreach(Player player in Game.Players){
                	if(PotatoHolder == player){
                        Rpc(nameof(PlayerExploded),player.Id,false);
                        break;
                    }
			    }
            }
            
            for(int i = 0; i < TagTimers.Length; i++){
                TagTimers[i] += fDelta;
            }
        }
	}

    public override void PlayerRespawned(Player player){
        //Player won't be invulnerable on respawn
    }

    public void OnLevelLoaded(){
        if(Online.IsHost()){
            SetBombTimer();
            GiveBomb();
        }
    }

    public static void IncreaseBombPitch(){
        const float PITCH_INCREASE = 0.075f;
        BombPitch += PITCH_INCREASE;
    }

	public void GiveBomb(){
        List<Player> eligiblePlayers = new List<Player>();
        foreach(Player player in Game.Players){
			if(!player.Finished) eligiblePlayers.Add(player);
        }
        
        if(eligiblePlayers.Count > 0){
            Player selectedPlayer = eligiblePlayers[Game.Random.Next(0, eligiblePlayers.Count)];
            Rpc(nameof(SyncPotatoHolder),selectedPlayer.Id);
        }else GD.Print("Error: No eligible players.");
    }

    //Returns Id of next player to get Bomb
    public void GiveBombClosest(){
        GD.Print("Giving!");
        List<Player> eligiblePlayers = new List<Player>();
        foreach(Player player in Game.Players){
            if(!player.Finished && player != PotatoHolder) eligiblePlayers.Add(player);
        }

        Player furthestPlayer = null;
        float furthestDistance = -1;
        foreach(Player player in eligiblePlayers){
            float distance = Math.Abs(player.Rb.GlobalPosition.DistanceTo(deathPoint));
            if(distance > furthestDistance){
                furthestPlayer = player;
                furthestDistance = distance;
            }
            if(distance <= 1024) player.PlayerEmotion = Player.Emotion.Shocked;
        }

        if(eligiblePlayers.Count > 0){
            if(eligiblePlayers.Count > 0) Rpc(nameof(SyncPotatoHolder),furthestPlayer.Id);
            else GD.Print("Error: No eligible players.");
        }
    }

	public static void PlayerLost(Player player,bool disconnected){
        Positions[player.Id-1] = (byte)(Game.TotalPlayers - deadPlayers);
        GD.Print(string.Join(",",Positions));
        if(Game.PlayerDatas[player.Id-1].VibrationEnabled && Game.MouseMode == Game.MouseModeEnum.Off && (!Online.IsOnline || player.OwnsPlayer())) Input.StartJoyVibration((int)player.PlayerData.InputDevice,1,1,1);
        player.Finished = true;
        deadPlayers++;
        if(deadPlayers >= Game.TotalPlayers - 1 && !Finished){
            for(byte i = 1; i <= Game.TotalPlayers; i++){
			    if(Positions[i-1] == 0){
                    Positions[i-1] = 1;
                    GD.Print(string.Join(",",Positions));
                    break;
                }
		    }
            GameFinished();
        }
    }

    protected override void SetPoints(){}

    public override void PlayerDisconnected(Player player){
        if(!player.Finished) PlayerExploded(player.Id,true);
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause){
        base.PlayerDied(player,deathCause);
        if(deathCause == Death.DeathCause.Offscreen){
            PlayerLost(player,false);
            if(player == PotatoHolder){
                SetBombTimer();
                GiveBomb();
            }
        }
    }

    public override void PlayerKilledPlayer(Player playerWhoDied, Player playerWhoKilled, Death.DeathCause deathCause){
        base.PlayerKilledPlayer(playerWhoDied, playerWhoKilled, deathCause);
        if(playerWhoKilled == PotatoHolder && playerWhoDied != PotatoHolder){ //If the stomper Player is It swap It
            Rpc(nameof(TaggedPlayer),playerWhoKilled.Id,playerWhoDied.Id);
			//playerWhoKilled.Rpc(nameof(playerWhoKilled.TaggedPlayer),playerWhoDied.Id,playerWhoKilled.Score,playerWhoDied.Score);
		}else{
			TagTimers[playerWhoDied.Id-1] = 0;
			TagTimers[playerWhoKilled.Id-1] = 0;
		}
    }

    public override void PlayerBumpedPlayer(Player bumper, Player bumped){
        base.PlayerBumpedPlayer(bumper, bumped);
        if(bumped == PotatoHolder && TagTimers[bumped.Id-1] >= 0.2f){ //If bumped is King swap crown to bumper
			Rpc(nameof(TaggedPlayer),bumped.Id,bumper.Id);
		}
    }

    public override float GetChargeMultiplier(Player player){
        return player == PotatoHolder ? 1.25f : 1; //Charge faster with bomb
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void TaggedPlayer(byte oldPotatoId, byte newPotatoId){
		Player newItPlayer = Game.Players[newPotatoId-1];
		PotatoHolder = newItPlayer;
		TagTimers[oldPotatoId-1] = 0;
        TagTimers[newPotatoId-1] = 0;

		SFX.Play("Orchestra",HotPotato.BombPitch); 
		IncreaseBombPitch();
	}

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncPotatoHolder(byte id){
        PotatoHolder = Game.Players[id-1];
    }

    public void SetBombTimer(){
        switch(Game.TotalPlayers){
            case 1: bombTimer = 4; break;
            case 2: bombTimer = 16 + (Game.Random.NextSingle() * (29-16)); break; //24 Second song
            case 3: bombTimer = 19 + (Game.Random.NextSingle() * (29-19)); break; //49 Second song
            case 4: bombTimer = 15 + (Game.Random.NextSingle() * (22-15)); break; //61 Second song
            case 5: bombTimer = 13 + (Game.Random.NextSingle() * (19-13)); break; //70 Second
            case 6: bombTimer = 11 + (Game.Random.NextSingle() * (17-11)); break; //80
            case 7: bombTimer = 11 + (Game.Random.NextSingle() * (17-11)); break; //91
            case 8: bombTimer = 9 + (Game.Random.NextSingle() * (17-9)); break; //96
        }
    }

    public override Item GiveItem(Player player){
        Tuple<Item, int>[] items;
        float deficit;
        if(player == PotatoHolder){
            deficit = 1-(bombTimer/20f);
            items = new Tuple<Item, int>[]{
                Tuple.Create((Item)new Wings(player), 10),
                Tuple.Create((Item)new BowlingBall(player), 9),
                Tuple.Create((Item)new Moon(player), 7),
                Tuple.Create((Item)new SmallBall(player), 7),
                Tuple.Create((Item)new Inverter(player), 6),
                Tuple.Create((Item)new Pepper(player,2), 4),
                Tuple.Create((Item)new StopSign(player,2), 3),
                Tuple.Create((Item)new Ball(player,2), 2),
                Tuple.Create((Item)new BigFungus(player), 1)
            };
        }else{
            deficit = Game.Random.NextSingle();
            if(deficit > 0.5f) deficit -= 0.5f;
            items = new Tuple<Item, int>[]{
                Tuple.Create((Item)new Booll(player), 12),
                Tuple.Create((Item)new Wings(player), 10),
                Tuple.Create((Item)new Ball(player,2), 9),
                Tuple.Create((Item)new Inverter(player), 8),
                Tuple.Create((Item)new SmallBall(player), 8),
                Tuple.Create((Item)new BowlingBall(player), 7),
                Tuple.Create((Item)new Moon(player), 5),
                Tuple.Create((Item)new Pepper(player,2), 4),
                Tuple.Create((Item)new StopSign(player,2), 2),
                Tuple.Create((Item)new BigFungus(player), 1)
            };
        }
        
        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PlayerExploded(byte explodedId,bool disconnected){//byte newItId
        //Explode Player
        GD.Print("Explosion");
        Player player = Game.Players[explodedId-1];
        //Node2D explosion = GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Explosion.tscn").Instantiate<Node2D>();
        //explosion.GlobalPosition = player.Rb.GlobalPosition;
        //AddChild(explosion);
        ParticleManager.ParticleManagerNode.SpawnExplosion(player.Rb.GlobalPosition,0.5f);
        deathPoint = player.Rb.GlobalPosition;
        if(Online.IsHost()){
            GD.Print("Explosion Host");
            PlayerLost(player,disconnected);
            if(player == PotatoHolder){
                GD.Print("Explosion it");
                SetBombTimer();
                GiveBombClosest();
            } 
        }else player.Finished = true;
        GD.Print("Explosion func done");
        BombPitch = STARTING_BOMB_PITCH;
    }
}