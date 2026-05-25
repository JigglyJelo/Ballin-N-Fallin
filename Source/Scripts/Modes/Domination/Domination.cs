using Godot;
using System;
using System.Collections.Generic;

public partial class Domination : Mode, IModeStartEvent{
	public static float TotalScore = 30;
    public static float TopScore;
	private readonly Palette[] DOMINATION_PALETTES = new Palette[]{
        new Palette(Color.Color8(240,240,240),Color.Color8(200,200,200),Colors.Black)//Color.Color8(64,64,64)
    };
	private List<DominationZone> zones = new List<DominationZone>();
    public static int ZoneCount;
    public const float FAST_MUSIC_SPEED = 1.25f;

    public override void _Ready(){
		Domination.Scores = new float[Game.TotalPlayers];
		LevelPalette = DOMINATION_PALETTES[Game.Random.Next(0,DOMINATION_PALETTES.Length)];
		base._Ready();
        Game.CurrentMode = Mode.GameMode.Domination;
        Instructions = "Control the zones";
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = true;
        Scores = new float[Game.TotalPlayers];
        TopScore = 0;
	}

    public void OnModeStart(){
        foreach(Node node in Level.LevelNode.GetChildren()){
			if(node is DominationZone zone){
				zones.Add(zone);
			}
		}
        ZoneCount = zones.Count;
    }

    public override void PlayerKilledPlayer(Player playerWhoDied, Player playerWhoKilled, Death.DeathCause deathCause){
        base.PlayerKilledPlayer(playerWhoDied, playerWhoKilled, deathCause);
		//Give all dead player's zones to killer player
		foreach(DominationZone zone in zones){
			if(zone.ControllingPlayer == playerWhoDied){
				//zone.ControllingPlayer = playerWhoKilled;
                zone.Rpc(nameof(zone.SyncControllingPlayer),playerWhoKilled.Id);
			}
		}
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause){
		base.PlayerDied(player,deathCause);
		//Lose all zones
		foreach(DominationZone zone in zones){
			if(zone.ControllingPlayer == player){
				//zone.ControllingPlayer = null;
                zone.Rpc(nameof(zone.SyncControllingPlayer),0);
			}
		}
    }

    public override string GetPlayerText(Player player){
        return (int)Domination.Scores[player.Id-1]+"/"+(int)Domination.TotalScore;
    }

    protected override void SetPoints(){
		int[] sortedScores = new int[Game.TotalPlayers];
        // Populate sortedScores
        for(int i = 0; i < Scores.Length; i++){
            sortedScores[i] = (int)Scores[i];
        }
    
        GD.Print("Domination Scores: " + string.Join(",", Scores));
    
        Array.Sort(sortedScores);
        Array.Reverse(sortedScores);
        GD.Print("Domination Sorted Scores: " + string.Join(",", sortedScores));

        for(int i = 0; i < Game.TotalPlayers; i++){
            Positions[i] = (byte)(Array.IndexOf(sortedScores, (int)Scores[i]) + 1);
        }
    }

	public override Item GiveItem(Player player){
		foreach(float score in Scores){
			if(score > TopScore) TopScore = score;
		}

        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Booll(player), 12),
            Tuple.Create((Item)new Wings(player), 10),
            Tuple.Create((Item)new BigFungus(player), 8),
            Tuple.Create((Item)new Moon(player), 7),
            Tuple.Create((Item)new StopSign(player,2), 6),
            Tuple.Create((Item)new BowlingBall(player), 5),
            Tuple.Create((Item)new Pepper(player,2), 5),
            Tuple.Create((Item)new Inverter(player), 5),
			Tuple.Create((Item)new StopSign(player,1), 4),
			Tuple.Create((Item)new Pepper(player,2), 3),
			Tuple.Create((Item)new Ball(player,3), 3),
			Tuple.Create((Item)new Ball(player,2), 2),
			Tuple.Create((Item)new Ball(player,1), 1),
			Tuple.Create((Item)new SmallBall(player), 1),
        };

        float maxScoreThreshold = TopScore;
        float deficit = 1 - (Domination.Scores[player.Id-1]/maxScoreThreshold);
        
        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}
}