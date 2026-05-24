using Godot;

public abstract partial class TeamSportsMode : TeamMode, ILevelLoadedEvent, IModeStartEvent, IRoundEndedEvent{
    protected static Label teamALabel, teamBLabel, goalLabel;
    public static byte TotalScore = 5;
    public static byte TeamAScore;
    public static byte TeamBScore;
    public static SportBall SportBall;

    public override void _Ready(){
        base._Ready();
        SportBall = null;
        //Create and setup HUD
        //Create and setup HUD
        CanvasLayer teamHud = GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/TeamHud.tscn").Instantiate<CanvasLayer>();
        teamHud.Scale =  new Vector2(Game.Resolution / 2160f,Game.Resolution / 2160f);
        AddChild(teamHud);
        teamALabel = GetNode<Label>("Team HUD/ATeamText");
        teamBLabel = GetNode<Label>("Team HUD/BTeamText");
        goalLabel = GetNode<Label>("Team HUD/GoalText");
        goalLabel.Text = "First to " + TotalScore;

        //Reset static variables
        WinningTeam = "";

        if(Instructions.Equals("")) Instructions = "Score " + TotalScore + " points";
        AddChild(GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = false;
    }

    public virtual void OnLevelLoaded(){
        SpawnBall("");
    }

    public override void OnModeStart(){
        base.OnModeStart();
        SetStartingScores();
        if(Online.IsHost()) SportBall.Rpc(nameof(SportBall.StartSpawnTween));
    }
    
    public void OnRoundEnd(){
        SportBall.GetNode<Node2D>("Smoothing2D").Visible = false;
        if(SportBall is TheBomb) SportBall.GetNode<Label>("Smoothing2D/Sprites/RightsideUp/Label").Visible = false;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public async void PointScored(string teamScoredOn){ //I really need to swap what the team parameter is cause currently its which team goal got scored on, not who to give point to
        OnPointScored(teamScoredOn);
        
        string losingTeam;
        if(TeamAScore < TeamBScore) losingTeam = "A";
        else if(TeamAScore > TeamBScore) losingTeam = "B";
        else losingTeam = "";
        
        
        if(teamScoredOn.Equals("A")){
            TeamBScore++;
        }else{
            TeamAScore++;
        }
        
        
        if(!losingTeam.Equals(teamScoredOn)){
            int teamScore = losingTeam.Equals("A") ? TeamAScore : TeamBScore;
            if(teamScore < TotalScore){
                MusicPlayer.SetPitch(1+(0.2f*((float)teamScore/(TotalScore-1))));
            }
        }
        
        foreach(Player player in Game.Players){
            if(player.Team.Equals(teamScoredOn)) player.PlayerEmotion = Player.Emotion.Annoyed;
            else player.PlayerEmotion = Player.Emotion.Happy;
        }
        if(Online.IsHost()){
            SportBall.Rb.SetDeferred("freeze",true);
            await ToSignal(GetTree().CreateTimer(0.017f,false), "timeout");
            SetBallOffscreen();
            SportBall.Rb.SetDeferred("freeze",true);
        }
        
        teamALabel.Text = TeamAScore.ToString();
        teamBLabel.Text = TeamBScore.ToString();
        //Check if game ended if not respawn ball
        if(Online.IsHost()){
            if(TeamAScore >= TotalScore && !Finished){
                WinningTeam = "A";
                SportBall.QueueFree();
                GameFinished();
            }else if(TeamBScore >= TotalScore && !Finished){
                WinningTeam = "B";
                SportBall.QueueFree();
                GameFinished();
            }else if(!Finished){
                await ToSignal(GetTree().CreateTimer(3f,false), "timeout");
                SpawnBall(teamScoredOn);
                SportBall.Rpc(nameof(SportBall.StartSpawnTween));
            }
        }else if(TeamAScore >= TotalScore && !Finished) WinningTeam = "A";
        else if(TeamBScore >= TotalScore && !Finished) WinningTeam = "B";
    }

    protected virtual void OnPointScored(string teamScoredOn){
        SFX.Play("Airhorn");
    }

    private void SetBallOffscreen(){
        SportBall.Rb.SetDeferred("freeze",false);
        SportBall.Rb.SetDeferred("linear_velocity",Vector2.Zero);
        SportBall.Rb.SetDeferred("global_position",new Vector2(0,100000));
        SportBall.Rb.SkipInterpolation();
        GD.Print(SportBall.Rb.GlobalPosition); //MAGIC PRINT THAT MAKES THIS WORK REMOVING THIS PRINT MAKES IT BREAK
    }

    //Give points to players on winning team
    protected override void SetPoints(){
        foreach(Player player in Game.Players){
            if(player.Team.Equals(WinningTeam)) Positions[player.Id - 1] = 1;
            else Positions[player.Id - 1] = (byte)Game.TotalPlayers;
        }
    }

    public void SpawnBall(string team){
        if(SportBall == null){
            if(Game.CurrentMode != GameMode.BombBall){
                SportBall = GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/SportBall.tscn").Instantiate<SportBall>();
            }else{
                SportBall = GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/Bomb Ball/BombBall.tscn").Instantiate<TheBomb>();
            }
            AddChild(SportBall);
        }
        Node2D sportBallSprite = SportBall.Smoother.GetNode<Node2D>("Sprites");
        sportBallSprite.Scale = Vector2.Zero;
        SportBall.Rb.SetDeferred("freeze",false);
        SportBall.Rb.SetDeferred("linear_velocity",Vector2.Zero);
        switch(Game.CurrentMode){
            default:
                SportBall.Rb.SetDeferred("global_position",Level.GetRandomRespawn()); 
                break;
            case GameMode.Volleyball:
                SportBall.Rb.SetDeferred("global_position",string.IsNullOrEmpty(team) ? new Vector2(0,100000) : Level.GetRandomRespawn(team)); 
                break;
        }
        SportBall.Rb.SkipInterpolation();
        GD.Print(SportBall.Rb.GlobalPosition);
    }

    public virtual void SetStartingScores(){
        if(Game.TotalPlayers % 2 == 0){
            TeamAScore = 0;
            TeamBScore = 0;
        }else if(Game.TotalPlayers == 3){
            if(TeamAPlayerCount > TeamBPlayerCount){
                TeamAScore = 0;
                TeamBScore = 2;
            }else{
                TeamAScore = 2;
                TeamBScore = 0;
            }
        }else{
            if(TeamAPlayerCount > TeamBPlayerCount){
                TeamAScore = 0;
                TeamBScore = 1;
            }else{
                TeamAScore = 1;
                TeamBScore = 0;
            }
        }
        teamBLabel.Text = TeamBScore.ToString();
    }
}