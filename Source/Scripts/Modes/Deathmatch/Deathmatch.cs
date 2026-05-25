using System;
using Godot;
using System.Collections.Generic;

public partial class Deathmatch : Mode{
	public static int TotalLives;
    public static int TopLives;
    private static int deadPlayers;
    public static float TotalTime = 0;
    private readonly Palette[] DEATHMATCH_PALETTES = new Palette[]{
        new Palette(new Color(0,125/255f,1),new Color(0,27/255f,1),Colors.Black)
    };
    public static int[] PlayerLives;

	public override void _Ready(){
        TotalLives = Game.TotalPlayers;
        LevelPalette = DEATHMATCH_PALETTES[Game.Random.Next(0,DEATHMATCH_PALETTES.Length)];
        Instructions = "Be the last Ball standing";
        base._Ready();
        Game.CurrentMode = Mode.GameMode.Deathmatch;
        deadPlayers = 0;
        Scores = new float[Game.MAX_PLAYERS];
        for(int i = 0; i < Scores.Length; i++) Scores[i] = float.MaxValue;
        isScoreMode = true;
        TotalTime = 0;
        TopLives = TotalLives;
        PlayerLives = new int[Game.TotalPlayers];
        for(int i = 0; i < PlayerLives.Length; i++) PlayerLives[i] = TotalLives;
	}

    public override void _PhysicsProcess(double delta){
        TotalTime += (float)delta;
    }

    public override void PlayerDied(Player player,Death.DeathCause deathCause){
        base.PlayerDied(player,deathCause);
        Death.DeathNode.Rpc(nameof(Death.DeathNode.RemoveLife),player.Id);
        if(PlayerLives[player.Id-1] <= 0){
            if(Online.IsHost()) Deathmatch.PlayerLost(player,Deathmatch.TotalTime);
            else player.Finished = true;
        }
    }

    public override void PlayerDisconnected(Player player){
        if(!player.Finished) PlayerLost(player,float.MinValue);
    }

    public override void PlayerRespawned(Player player){
        base.PlayerRespawned(player);
        player.ShowPlayerText();
        //if(Online.IsHost()) player.Rpc(nameof(player.ShowPlayerText));
    }

    public static void PlayerLost(Player player,float playersTime){
        deadPlayers++;
        Scores[player.Id-1] = (TotalTime - playersTime < 1) ? playersTime : TotalTime;

        if(deadPlayers >= Game.TotalPlayers - 1){
            if(Online.IsHost()) GameFinished();
        }
        player.Finished = true;
    }

    public override string GetPlayerText(Player player){
        return Deathmatch.PlayerLives[player.Id-1] + ((Deathmatch.PlayerLives[player.Id-1] == 1) ? " Life" : " Lives");
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
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new BigFungus(player), 11),
            Tuple.Create((Item)new Wings(player), 9),
            Tuple.Create((Item)new BowlingBall(player), 8),
            Tuple.Create((Item)new Booll(player), 8),
            Tuple.Create((Item)new StopSign(player,2), 7),
            Tuple.Create((Item)new Ball(player,2), 6),
            Tuple.Create((Item)new Inverter(player), 4),
            Tuple.Create((Item)new Moon(player), 3),
            Tuple.Create((Item)new Pepper(player,2), 2),
            Tuple.Create((Item)new SmallBall(player), 1),
        };

        float lifeDifference = MathF.Abs(PlayerLives[player.Id-1] - TopLives);
        
        float lifeDeficit = lifeDifference / (TotalLives - 1);
        float comebackScore = GetItemLuck(lifeDeficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}
}