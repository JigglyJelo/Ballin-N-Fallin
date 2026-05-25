using Godot;
using System;
using System.Collections.Generic;

public partial class Race : Mode, ILevelLoadedEvent{
    public static int TopLap;
	public static int TotalLaps;
    public static float RaceTimer;
    public static float[] Times;
    public static int TopCheckPoint;
    private Label raceTimerLabel;
    private Path2D racePath;
    private Vector2[] racePathBakedPoints;
    private readonly Palette[] RACE_PALETTES = new Palette[]{
        new Palette(new Color(1,173/255f,33/255f),new Color(1,128/255f,33/255f),new Color(255/255f,97/255f,0))
    };
    public static int[] PlayerCheckpoints;
    public static int[] PlayerLaps;

	public override void _Ready(){
        LevelPalette = RACE_PALETTES[Game.Random.Next(0,RACE_PALETTES.Length)];
        base._Ready();
        Level level = GetNode<Level>("Level");
        racePath = level.GetNode<Path2D>("RacePath");
        racePath.Curve.BakeInterval = 200;
        TotalLaps = level.LevelUnit;
        Instructions = "Complete " + TotalLaps + " Laps";
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        RaceCheckpoint.TotalCheckpoints = 0;
        Node2D levelNode = GetNode<Node2D>("Level");
        RaceCheckpoint.TotalCheckpoints = 0;
        foreach(Node node in levelNode.GetChildren()){
            if(node is RaceCheckpoint) RaceCheckpoint.TotalCheckpoints++;
        }
        Game.CurrentMode = GameMode.Race;
        
        RaceTimer = 0;
        
        TopCheckPoint = 0;
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/Race/Countdown.tscn").Instantiate());

        //Time Trials
        if(Game.TotalPlayers == 1){
            AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/Race/RaceHUD.tscn").Instantiate());
            //Game.Players[0].Item = new Pepper(Game.Players[0],TotalLaps); Moved to Level Spawn Player function
        }
        isScoreMode = true;
        racePathBakedPoints = racePath.Curve.GetBakedPoints();
        for(int i = 0; i < racePathBakedPoints.Length; i++) racePathBakedPoints[i] += levelNode.GlobalPosition;
        Scores = new float[Game.MAX_PLAYERS];
        for(int i = 0; i < Scores.Length; i++) Scores[i] = float.MaxValue;
        PlayerCheckpoints = new int[Game.TotalPlayers];
        PlayerLaps = new int[Game.TotalPlayers];
        TopLap = 0;
	}

	public override void _PhysicsProcess(double delta){
		if(Engine.TimeScale == 1.0) RaceTimer += (float)delta;
	}

    public void OnLevelLoaded(){
        if(Game.TotalPlayers == 1)
            Game.Players[0].Item = new Pepper(Game.Players[0],(byte)Level.LevelNode.LevelUnit);
        else
            foreach(Player player in Game.Players) player.Invulnerable = true;
    }


    public override void PlayerDied(Player player,Death.DeathCause deathCause){
        foreach(Node node in Level.LevelNode.GetChildren()){
			if(node is RaceCheckpoint checkpoint && checkpoint.id == PlayerCheckpoints[player.Id-1]){
				player.SpawnPoint = checkpoint.GetNode<Node2D>("CollisionShape2D").GlobalPosition;
				break;
			}
		}
    }

    public override void PlayerDisconnected(Player player){
        if(!player.Finished) PlayerFinished(player,float.PositiveInfinity);
    }

    public static void PlayerFinished(Player player,float playersTime){
        if(playersTime == float.PositiveInfinity) Scores[player.Id-1] = float.PositiveInfinity;
        else Scores[player.Id-1] = (RaceTimer - playersTime < 1) ? playersTime : RaceTimer;
        player.Finished = true;
        RaceCheckpoint.FinishedPlayers++;
    }

    public override string GetPlayerText(Player player){
        return "Lap " + (int)(Race.PlayerLaps[player.Id-1] + 1) + "/" + Race.TotalLaps;
    }


    private Line2D line;
    private Line2D remainingLine = null;
    public override Item GiveItem(Player player){
        int playerCheckpoint = PlayerCheckpoints[player.Id-1];
        int playerLap = PlayerLaps[player.Id-1];
        float itemValue;
        int playerStrength = 1;
		int topStrength = 1;
		playerStrength += playerLap * RaceCheckpoint.TotalCheckpoints;
		playerStrength += PlayerCheckpoints[player.Id-1];
		topStrength += TopLap * RaceCheckpoint.TotalCheckpoints;
		topStrength += TopCheckPoint;
		itemValue = topStrength / playerStrength;
        float playerLapDistance = getLength(racePathBakedPoints) * (playerLap-1);
        if(playerLapDistance < 0) playerLapDistance = 0;
        int startingIndex = indexOfClosestPoint(racePathBakedPoints,player.Rb.GlobalPosition);
        if(startingIndex == -1) startingIndex = 0;
        
        int endingIndex = -1;
        Player firstPlacePlayer = null;
        bool playerWithHigherLapCountExists = false;
        int highestLap = 0;
        foreach(Player comparisonPlayer in Game.Players){
            int comparisonPlayerCheckpoint = PlayerCheckpoints[comparisonPlayer.Id-1];
            int comparisonPlayerLap = PlayerLaps[comparisonPlayer.Id-1];
            if(!comparisonPlayer.Finished && (comparisonPlayerLap > playerLap || (comparisonPlayerLap == playerLap && comparisonPlayerCheckpoint >= playerCheckpoint))){
                int potentialIndex = indexOfClosestPoint(racePathBakedPoints,comparisonPlayer.Rb.GlobalPosition);
                if(comparisonPlayerLap > playerLap && comparisonPlayerLap >= highestLap){ //Doesnt account for if player just completes new lap but goes backward then it basically gives an extra lap of distance
                    startingIndex = 0;
                    highestLap = comparisonPlayerLap;
                    playerWithHigherLapCountExists = true;
                    if(potentialIndex >= endingIndex){
                        endingIndex = potentialIndex;
                        firstPlacePlayer = comparisonPlayer;
                    }
                }else if(!playerWithHigherLapCountExists && comparisonPlayerLap == playerLap && comparisonPlayerCheckpoint >= playerCheckpoint){
                    if(potentialIndex > endingIndex){
                        endingIndex = potentialIndex;
                        firstPlacePlayer = comparisonPlayer;
                    }
                }
            }
        }
        if(endingIndex == -1) endingIndex = racePathBakedPoints.Length-1;
        
        if(line != null) line.QueueFree();
        line = new Line2D();
        line.Name = "VisualLine";
        
        GD.Print(startingIndex + " " + endingIndex + " " + racePathBakedPoints.Length);
        for(int i = startingIndex; i <= endingIndex; i++){
            line.AddPoint(racePathBakedPoints[i]);
        }
        line.GlobalPosition -= Level.LevelNode.GlobalPosition;
        if(line.Points.Length == 0) line.AddPoint(new Vector2(100000,100000));
        if(line.Points.Length == 1) line.AddPoint(line.Points[0]);
        line.SelfModulate = new Color(firstPlacePlayer.PlayerColor,0.5f);
        line.Visible = false;
        Level.LevelNode.AddChild(line);
        int firstPlaceLap = PlayerLaps[firstPlacePlayer.Id-1];
        float topLapDistance = getLength(racePathBakedPoints) * (firstPlaceLap-1);
        if(topLapDistance < 0) topLapDistance = 0;
        float playerRemainingLapDistance = 0;
        if(remainingLine != null){
            remainingLine.QueueFree();
            remainingLine = null;
        }
        if(playerWithHigherLapCountExists){
            int playerPositionIndex = indexOfClosestPoint(racePathBakedPoints,player.Rb.GlobalPosition);
            remainingLine = new Line2D();
            remainingLine.Name = "RemLine";
        
            for(int i = playerPositionIndex; i < racePathBakedPoints.Length; i++){
                remainingLine.AddPoint(racePathBakedPoints[i]);
            }
            remainingLine.GlobalPosition -= Level.LevelNode.GlobalPosition;
            if(line.Points.Length == 0) remainingLine.AddPoint(new Vector2(100000,100000));
            if(line.Points.Length == 1) remainingLine.AddPoint(remainingLine.Points[0]);
            playerRemainingLapDistance = getLength(remainingLine.Points);
            remainingLine.SelfModulate = new Color(player.PlayerColor,0.5f);
            remainingLine.Visible = false;
            Level.LevelNode.AddChild(remainingLine);
        }
        
        float lapDistance = topLapDistance - (playerLapDistance-playerRemainingLapDistance);
        if((int)Math.Abs(playerLap - firstPlaceLap) == 1){
            lapDistance = playerRemainingLapDistance;
        }
        float distance = getLength(line.Points);
        GD.Print("dist between 1st and start or u: " + distance + " Your lap dist: " + playerLapDistance + " Ur Remaining lap: " + playerRemainingLapDistance);
		itemValue = distance + lapDistance;
        GD.Print("End total dist: " + itemValue);
        float raceDeficit = itemValue / 12500;
        float comebackScore = GetItemLuck(raceDeficit,GetComebackLuck(player));
    
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Wings(player), 10),
            Tuple.Create((Item)new BowlingBall(player), 9),
            Tuple.Create((Item)new Moon(player), 7),
            Tuple.Create((Item)new SmallBall(player), 7),
            Tuple.Create((Item)new Inverter(player), 6),
            Tuple.Create((Item)new Booll(player), 5),
            Tuple.Create((Item)new Pepper(player,2), 4),
            Tuple.Create((Item)new StopSign(player,2), 3),
            Tuple.Create((Item)new Ball(player,2), 2),
            Tuple.Create((Item)new BigFungus(player), 1)
        };
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
        int indexOfClosestPoint(Vector2[] points, Vector2 point){
            Vector2 closestPoint = points[0];
            int indexToReturn = 0;
            for(int i = 1; i < points.Length; i++){
                if(points[i].DistanceSquaredTo(point) < closestPoint.DistanceSquaredTo(point)){
                    closestPoint = points[i];
                    indexToReturn = i;
                }
            }
            return indexToReturn;
        }
        float getLength(Vector2[] line){
            if(line.Length < 2) return 0;
            float length = 0;
            for(int i = 0; i < line.Length - 1; i++){
                length += line[i].DistanceTo(line[i + 1]);
            }
            return length;
        }
    }

    protected override void SetPoints(){
        float[] sortedScores = (float[])Scores.Clone();
        Array.Sort(sortedScores);
        GD.Print(string.Join(",",sortedScores));
        //First loop goes through each index of sortedScores
        for(int i = 0; i < Game.TotalPlayers; i++){
            //Second loop compares each players score to the current array index
            for(int j = 0; j < Game.TotalPlayers; j++){
                if(sortedScores[i] == Scores[j]){
                    Positions[j] = (byte)(i + 1);
                    break;
                }
            }
        }
    }
}