using Godot;
using System;
using System.Collections.Generic;

public partial class Survival : Mode{
	public static int TotalLives = 1;
    private static int deadPlayers;
    public static float TotalTime = 0;
    public static float Timer = 0;
    public static int[] PlayerLives;
	public override void _Ready(){
        Instructions = GetInstructionsText();
        base._Ready();
        Game.CurrentMode = Mode.GameMode.Survival;
        deadPlayers = 0;
        TotalTime = 0;
        Timer = 0;

        //Endless Solo
        if(Game.TotalPlayers == 1){ 
            AddChild(GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Survival/SurvivalHUD.tscn").Instantiate());
        }
        isScoreMode = true;
        Scores = new float[Game.MAX_PLAYERS];
        for(int i = 0; i < Scores.Length; i++) Scores[i] = float.MaxValue;
        PlayerLives = new int[Game.TotalPlayers];
        for(int i = 0; i < PlayerLives.Length; i++) PlayerLives[i] = TotalLives;
	}

    public override void _PhysicsProcess(double delta){
        float fDelta = (float)delta;
        Timer += fDelta;
        TotalTime += fDelta;
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause){
        base.PlayerDied(player,deathCause);
        Death.DeathNode.Rpc(nameof(Death.DeathNode.RemoveLife),player.Id);
        if(PlayerLives[player.Id-1] <= 0){
            if(Online.IsHost()) Survival.PlayerLost(player,Survival.TotalTime);
            else player.Finished = true;
        }
    }

    public override void PlayerDisconnected(Player player){
        if(!player.Finished) PlayerLost(player,float.MinValue);
    }

    public override void PlayerRespawned(Player player){
        base.PlayerRespawned(player);
        player.Visuals.ShowPlayerText();
    }

    public static void PlayerLost(Player player,float playersTime){
        deadPlayers++;
        Scores[player.Id-1] = (TotalTime - playersTime < 1) ? playersTime : TotalTime;
        
        if(deadPlayers >= Game.TotalPlayers - 1){
            if(Online.IsHost()){
                if(Game.CurrentLevelName.Equals("TrashCompactor.tscn")){
                }
                GameFinished();
            }
        }
        player.Finished = true;
    }

    protected override void SetPoints(){
        float[] sortedScores = (float[])Scores.Clone();
        Array.Sort(sortedScores);  // Sorts in ascending order by default

        // Used to track which scores have been assigned positions
        bool[] scoreAssigned = new bool[Game.TotalPlayers];

        // Loop through the sorted scores
        for (int i = 0; i < Game.TotalPlayers; i++){
            // Loop through the original scores to find players with matching scores
            for(int j = 0; j < Game.TotalPlayers; j++){
                // If the player's score matches the current sorted score and hasn't been assigned yet
                if(Math.Abs(sortedScores[i] - Scores[j]) < 0.001 && !scoreAssigned[j]){
                    Positions[j] = (byte)(Game.TotalPlayers - i); // Assign the position
                    scoreAssigned[j] = true;  // Mark this score as assigned
                }
            }
        }
    }

    public override Item GiveItem(Player player){
        float comebackScore = GetItemLuck(Game.Random.NextSingle(),GetComebackLuck(player));
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Wings(player), 10),
            Tuple.Create((Item)new Moon(player), 9),
            Tuple.Create((Item)new Inverter(player), 9),
            Tuple.Create((Item)new SmallBall(player), 7),
            Tuple.Create((Item)new BowlingBall(player), 7),
            Tuple.Create((Item)new Booll(player), 5),
            Tuple.Create((Item)new StopSign(player,2), 5),
            Tuple.Create((Item)new BigFungus(player), 4),
            Tuple.Create((Item)new Pepper(player,2), 3),
            Tuple.Create((Item)new Ball(player,2), 2)
        };
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
    }

    private static string GetInstructionsText(){
        if(Game.CurrentFolderPath.Contains("Trash Compactor")){
            return "Dodge the trash";
        }else if(Game.CurrentFolderPath.Contains("Sumo")){
            return "Stay in the ring";
        }else{
            return "Survive";
        }
    }
}