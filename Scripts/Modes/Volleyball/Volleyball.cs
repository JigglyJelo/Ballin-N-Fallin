using Godot;
using System;
using System.Collections.Generic;

public partial class Volleyball : TeamSportsMode, ILevelLoadedEvent{
    public static PackedScene VolleyBall;
    private readonly Palette[] VOLLEYBALL_PALETTES = new Palette[]{
        new Palette(new Color(1,1,207/255f),new Color(1,1,149/255f),new Color(1,200/255f,0))
    };
    
    public override void _Ready(){
        TotalScore = 5;
        LevelPalette = VOLLEYBALL_PALETTES[Game.Random.Next(0,VOLLEYBALL_PALETTES.Length)];
        base._Ready();
        Game.CurrentMode = Mode.GameMode.Volleyball;
    }

    public override void OnLevelLoaded(){
        if(Online.IsHost()){
            if(Game.Random.Next(0,2) == 0) SpawnBall("A");
            else SpawnBall("B");
        }else{
            SpawnBall("");
        }
    }

    protected override void OnPointScored(string teamScoredOn){
        SFX.Play("Airhorn");
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause)
    {
        player.SpawnPoint = Level.GetRandomRespawn(player.Team);
    }

    public override Item GiveItem(Player player){
        Item item;
        int pointDifference;
        bool losingTeam = false;
        switch(player.Team){
            case "A":
                if(TeamAScore < TeamBScore) losingTeam = true;
                break;
            case "B":
                if(TeamAScore > TeamBScore) losingTeam = true;
                break;
        }

        pointDifference = (int)MathF.Abs(TeamAScore - TeamBScore);

        float teamDeficit = 0;
        if(losingTeam) teamDeficit = (float)pointDifference / TotalScore - 1;
        

        float comebackScore = GetItemLuck(teamDeficit,GetComebackLuck(player));
        
        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Wings(player), 10),
            Tuple.Create((Item)new BigFungus(player), 9),
            Tuple.Create((Item)new StopSign(player,2), 8),
            Tuple.Create((Item)new Moon(player), 8),
            Tuple.Create((Item)new Inverter(player), 6),
            Tuple.Create((Item)new Booll(player), 5),
            Tuple.Create((Item)new Ball(player,2), 5),
            Tuple.Create((Item)new Pepper(player,2), 4),
            Tuple.Create((Item)new BowlingBall(player), 3),
            Tuple.Create((Item)new SmallBall(player), 1)
        };
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        item = SelectWeightedItem(items, probabilities);
        return item;
    }
}