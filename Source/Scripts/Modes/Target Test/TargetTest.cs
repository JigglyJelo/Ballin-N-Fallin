using Godot;
using System;
using System.Collections.Generic;

public partial class TargetTest : Mode, IModeStartEvent, ILevelLoadedEvent{
    public static int TopScore;
    public static int TotalScore = 250;
    public static int[] PlayerScores;
    public List<TargetZone> TargetZones = new List<TargetZone>();

    public override void _Ready(){
        base._Ready();
        Scores = new float[Game.MAX_PLAYERS];
        PlayerScores = new int[Game.TotalPlayers];
        Game.CurrentMode = Mode.GameMode.TargetTest;
        Instructions = "Land in the zones and get " + TotalScore + " points";
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = true;
        TopScore = 0;
	}

    public override string GetPlayerText(Player player){
        return PlayerScores[player.Id-1] + "/" + TotalScore;
    }

    public override void PlayerRespawned(Player player){
        base.PlayerRespawned(player);
        player.Visuals.ShowPlayerText();
    }
    
    public void OnModeStart(){
        if(Online.IsHost()){
            foreach(TargetZone zone in TargetZones){
                zone.UpdatePointValue();
            }
        }
    }

	public void OnLevelLoaded(){
		foreach(Player player in Game.Players) player.Invulnerable = true;
	}

    protected override void SetPoints(){
        int[] sortedScores = new int[Game.MAX_PLAYERS];
        // Populate Scores and sortedScores
        foreach(Player player in Game.Players){
            Scores[player.Id-1] = PlayerScores[player.Id-1];
            sortedScores[player.Id-1] = PlayerScores[player.Id-1];
        }
    
        GD.Print("TT Scores: " + string.Join(",", PlayerScores));
    
        Array.Sort(sortedScores);
        Array.Reverse(sortedScores);
        GD.Print("TT Sorted Scores: " + string.Join(",", sortedScores));

        for(int i = 0; i < Game.TotalPlayers; i++){
            Positions[i] = (byte)(Array.IndexOf(sortedScores, PlayerScores[i]) + 1);
        }
    }

	public override bool IsSamePlayerSpawns(){
		return true;
	}

    public override Item GiveItem(Player player){
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Wings(player), 10),
            Tuple.Create((Item)new BowlingBall(player), 8),
            Tuple.Create((Item)new StopSign(player,2), 8),
            Tuple.Create((Item)new Ball(player,2), 7),
            Tuple.Create((Item)new Moon(player), 6),
            Tuple.Create((Item)new Inverter(player), 5),
            Tuple.Create((Item)new Booll(player), 5),
            Tuple.Create((Item)new StopSign(player,1), 5),
            Tuple.Create((Item)new SmallBall(player), 4),
            Tuple.Create((Item)new Ball(player,1), 3),
            Tuple.Create((Item)new Pepper(player,2), 2),
            Tuple.Create((Item)new Pepper(player,1), 2),
            Tuple.Create((Item)new BigFungus(player), 1),
        };

        float maxScoreThreshold = TopScore > TotalScore-3 ? TotalScore-3 : TopScore;
        float deficit = 1 - (PlayerScores[player.Id-1]/maxScoreThreshold);
        
        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}
}