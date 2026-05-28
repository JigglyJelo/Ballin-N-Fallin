using Godot;
using System;
using System.Collections.Generic;

public partial class CTK : Mode, ILevelLoadedEvent, IModeStartEvent{
    public static float TopScore;
	public static int TotalScore = 30;
	public static PackedScene CrownScene;
    public Crown CrownNode;
    public const float CROWN_ANGLE = (float)(Math.PI / 20);
    private const float MIN_CROWN_PITCH = 0.5f;
    public static float CrownPitch = MIN_CROWN_PITCH;
    private static float pitchTimer = 0;
    private const float PITCH_RESET_TIME = 2;
    public const float FAST_MUSIC_SPEED = 1.25f;
    private readonly Palette[] CTK_PALETTES = new Palette[]{
        new Palette(new Color(1,238/255f,1),new Color(1,202/255f,1),new Color(210/255f,160/255f,210/255f))
    };
    public float[] TagTimers = new float[Game.TotalPlayers];
    private Player king;
    public static Player King{
		get{return (Mode.ModeNode as CTK).king;}
		set{
            CTK modeNode = Mode.ModeNode as CTK;
            if(modeNode.king != null){
                //Remove old king's crown and points
                Sprite2D crownSprite = modeNode.king.Visuals.SpritesNode.GetNodeOrNull<Sprite2D>("Crown");
                crownSprite.Visible = false;
				float timeToLose;
				switch(Game.TotalPlayers){
					case 2:
        				timeToLose = 5;
        				break;
    				case 3:
						timeToLose = 3;
						break;
					case 4:
        				timeToLose = 2;
        				break;
    				case 5:
						timeToLose = 1.5f;
						break;
    				case 6:
    				    timeToLose = 1.25f;
    				    break;
    				default:
    				    timeToLose = 1;
    				    break;
				}
				if(modeNode.king.Score >= CTK.TotalScore - timeToLose){
					modeNode.king.Score = CTK.TotalScore - timeToLose;
					modeNode.king.PlayerEmotion = Player.Emotion.Angry;
					modeNode.king.Visuals.ShowPlayerText();
				}else modeNode.king.PlayerEmotion = Player.Emotion.Sad;
            }
            //Set new King
            modeNode.king = value;
            if(value != null){
                Sprite2D crownSprite = modeNode.king.Visuals.SpritesNode.GetNodeOrNull<Sprite2D>("Crown");
                crownSprite.Visible = true;
				modeNode.king.PlayerEmotion = Player.Emotion.Happy;
                if(modeNode.king.Score >= TotalScore*(2.0/3.0)) MusicPlayer.SetPitch(FAST_MUSIC_SPEED);
                else if(MusicPlayer.GetPitch() != 1) MusicPlayer.SetPitch(1);
            }
        }
    }
    //public static float[] Scores;
	
	public override void _Ready(){
        LevelPalette = CTK_PALETTES[Game.Random.Next(0,CTK_PALETTES.Length)];
        base._Ready();
        Mode.Scores = new float[Game.MAX_PLAYERS];
        Game.CurrentMode = Mode.GameMode.CrownTheKing;
        Instructions = "Down the Crown for " + TotalScore + " seconds";
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = true;
        SpawnCrown();
        pitchTimer = 0;
        CrownPitch = MIN_CROWN_PITCH;
        King = null;
        TopScore = 0;
	}

    public override void _Process(double delta){
        if(CrownPitch > MIN_CROWN_PITCH){
            pitchTimer += (float)delta;
            if(pitchTimer >= PITCH_RESET_TIME){
                CrownPitch = MIN_CROWN_PITCH;
                pitchTimer = 0;
            }
        }
    }

    public override void _PhysicsProcess(double delta){
        float fDelta = (float)delta;
        for(int i = 0; i < TagTimers.Length; i++){
            TagTimers[i] += fDelta;
        }
        if(King != null){
            if(King.Score <= CTK.TotalScore) King.Score += fDelta;
			if(King.Score >= CTK.TotalScore * (2.0/3.0) && MusicPlayer.GetPitch() != CTK.FAST_MUSIC_SPEED) MusicPlayer.SetPitch(CTK.FAST_MUSIC_SPEED);
			King.Visuals.ShowPlayerText();
			if(Online.IsHost() && King.Score >= CTK.TotalScore && !Mode.Finished) Mode.GameFinished();
        }
    }

    public void OnLevelLoaded(){
        RigidBody2D crown = GetNode<RigidBody2D>("Crown");
        foreach(Player player in Game.Players) crown.AddCollisionExceptionWith(player.Rb);
    }

    public void OnModeStart(){
        foreach(Player player in Game.Players){
            Sprite2D crownSprite = CTK.GetCrownSprite(false);
			crownSprite.Visible = false;
			player.Visuals.SpritesNode.AddChild(crownSprite);
        }
    }

    public override void PlayerDisconnected(Player player){
        player.Finished = true;
		if(player == King){
            if(Online.IsHost()) Rpc(nameof(SpawnCrown));
            King = null;
        }
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause){
        base.PlayerDied(player,deathCause);
        if(deathCause == Death.DeathCause.Offscreen && player == King){
            SpawnCrown();
            King = null;
            if(player.Score > CTK.TotalScore * 0.9f) player.Score = CTK.TotalScore * 0.9f;
        }
    }

    public override void PlayerKilledPlayer(Player playerWhoDied, Player playerWhoKilled, Death.DeathCause deathCause){
        base.PlayerKilledPlayer(playerWhoDied, playerWhoKilled, deathCause);
        if(deathCause == Death.DeathCause.Stomp){
            if(playerWhoDied == King && playerWhoKilled != King){ //If stomped on Player is It swap It
                Rpc(nameof(TaggedPlayer),playerWhoDied.Id,playerWhoDied.Score,playerWhoKilled.Id,playerWhoKilled.Score);
			}else{
				TagTimers[playerWhoDied.Id-1] = 0;
				TagTimers[playerWhoKilled.Id-1] = 0;
			}
        }
    }

    public override void PlayerBumpedPlayer(Player bumper, Player bumped){
        base.PlayerBumpedPlayer(bumper, bumped);
        if(bumped == King && TagTimers[bumped.Id-1] >= 0.2f){ //If bumped is King swap crown to bumper
			Rpc(nameof(TaggedPlayer),bumped.Id,bumped.Score,bumper.Id,bumper.Score);
		}
    }

    public override string GetPlayerText(Player player){
        return (int)player.Score + "/" + TotalScore;
    }

    public override float GetChargeMultiplier(Player player){
        return player == King ? 0.9f : 1; //Charge slower with crown
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void TaggedPlayer(byte oldKingId, float oldKingScore, byte newKingId,float newKingScore){
        Player oldItPlayer = Game.Players[oldKingId-1];
		Player newItPlayer = Game.Players[newKingId-1];
        King = newItPlayer;
		TagTimers[oldKingId-1] = 0;
        TagTimers[newKingId-1] = 0;
		//Sync scores for online
		if(!Online.IsHost()){
			oldItPlayer.Score = oldKingScore;
			newItPlayer.Score = newKingScore;
		}
		SFX.Play("Orchestra",CTK.CrownPitch);
		CTK.IncreaseCrownPitch();
	}

    public static Sprite2D GetCrownSprite(bool flip){
		Sprite2D crownSprite = new Sprite2D();
		crownSprite.Name = "Crown";
		crownSprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/Mode Stuff/Crown the King/WearingCrowns/WearingCrown" + (Game.Resolution >= 720 ? Game.Resolution : 720) +".png");
		float crownScale = 114f/crownSprite.Texture.GetHeight();
		crownSprite.Scale = new Vector2(crownScale,crownScale);
		crownSprite.Rotation = CROWN_ANGLE;
		crownSprite.Position = new Vector2(-10,-100);
		crownSprite.FlipH = flip;
		int sign = flip ? 1 : -1;
		crownSprite.Position = new Vector2(10*sign,-100);
		crownSprite.Rotation = CROWN_ANGLE * sign;
		return crownSprite;
	}

    protected override void SetPoints(){
        float[] sortedScores = new float[Game.MAX_PLAYERS];
        // Populate Scores and sortedScores
        foreach(Player player in Game.Players){
            Scores[player.Id-1] = (int)player.Score;
            sortedScores[player.Id-1] = (int)player.Score;
        }
    
        GD.Print("KOTH Scores: " + string.Join(",", Scores));
    
        Array.Sort(sortedScores);
        Array.Reverse(sortedScores);
        GD.Print("KOTH Sorted Scores: " + string.Join(",", sortedScores));

        for(int i = 0; i < Game.TotalPlayers; i++){
            Positions[i] = (byte)(Array.IndexOf(sortedScores, Scores[i]) + 1);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SpawnCrown(){
		//Make Crown at spawnpoint
        if(CrownScene == null) CrownScene = GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/Crown the King/Crown.tscn");
        CrownNode = CrownScene.Instantiate<Crown>();
        AddChild(CrownNode);
	}

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DeleteCrown(){
        if(CrownNode != null){
            CrownNode.QueueFree();
            CrownNode = null;
            SFX.Play("Orchestra",CTK.CrownPitch);
            IncreaseCrownPitch();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncKing(byte id){
        King = Game.Players[id-1];
    }

    public override Item GiveItem(Player player){
        foreach(Player aPlayer in Game.Players){
            if(aPlayer.Score > TopScore) TopScore = aPlayer.Score;
        }
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Booll(player), 12),
            Tuple.Create((Item)new Wings(player), 10),
            Tuple.Create((Item)new BowlingBall(player), 9),
            Tuple.Create((Item)new Inverter(player), 8),
            Tuple.Create((Item)new SmallBall(player), 7),
            Tuple.Create((Item)new BigFungus(player), 7),
            Tuple.Create((Item)new Ball(player,2), 6),
            Tuple.Create((Item)new Moon(player), 4),
            Tuple.Create((Item)new StopSign(player,2), 3),
            Tuple.Create((Item)new Pepper(player,2), 3),
        };

        float maxScoreThreshold = TopScore;// > 25 ? 25 : TopScore;
        float deficit = 1 - (player.Score/maxScoreThreshold);
        if(player == King && deficit > 0.1f){
            deficit -= 0.1f;
        }

        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}

    public static void IncreaseCrownPitch(){
        const float MIN_PITCH_INCREASE = 0.075f;
        float pitchIncrease = 0.25f * (pitchTimer/PITCH_RESET_TIME);
        if(pitchIncrease < MIN_PITCH_INCREASE) pitchIncrease = MIN_PITCH_INCREASE;
        CrownPitch += pitchIncrease;
        pitchTimer = 0;
    }
}