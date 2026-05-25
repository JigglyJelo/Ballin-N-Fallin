using Godot;
using System;

public partial class KOTH : Mode{
	public static int TotalScore = 30;
    public static float TopScore;
    private readonly Palette[] KOTH_PALETTES = new Palette[]{
        new Palette(new Color(0,250/255f,0),new Color(0,195/255f,0),new Color(0,125/255f,0))
    };
    public const float FAST_MUSIC_SPEED = 1.25f;

	public override void _Ready(){
        if(Game.TotalPlayers >= 6) TotalScore = 20;
        if(Game.TotalPlayers >= 4) TotalScore = 25;
        else if(Game.TotalPlayers == 3) TotalScore = 27;
        else TotalScore = 30;
        LevelPalette = KOTH_PALETTES[Game.Random.Next(0,KOTH_PALETTES.Length)];
        base._Ready();
        Scores = new float[Game.MAX_PLAYERS];
        Game.CurrentMode = Mode.GameMode.KingOfTheHill;
        Instructions = "Stay in the zone for " + TotalScore + " seconds";
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = true;
        TopScore = 0;
	}

    public override string GetPlayerText(Player player){
        return (int)player.Score + "/" + KOTH.TotalScore;
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

    public override Item GiveItem(Player player){
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Booll(player), 12),
            Tuple.Create((Item)new BigFungus(player), 10),
            Tuple.Create((Item)new Wings(player), 9),
            Tuple.Create((Item)new BowlingBall(player), 8),
            Tuple.Create((Item)new SmallBall(player), 7),
            Tuple.Create((Item)new Ball(player,2), 7),
            Tuple.Create((Item)new StopSign(player,2), 6),
            Tuple.Create((Item)new Moon(player), 4),
            Tuple.Create((Item)new Pepper(player,2), 3),
            Tuple.Create((Item)new Inverter(player), 1),
        };

        float maxScoreThreshold = TopScore > TotalScore-3 ? TotalScore-3 : TopScore;
        float deficit = 1 - (player.Score/maxScoreThreshold);
        
        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}
}