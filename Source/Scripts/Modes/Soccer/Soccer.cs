using Godot;
using System;
using System.Collections.Generic;

public partial class Soccer : TeamSportsMode{
    private readonly Palette[] SOCCER_PALETTES = new Palette[]{
        new Palette(new Color(0,145/255f,0),new Color(0,200/255f,0),new Color(0,64/255f,0))
    };
    
    public override void _Ready(){
        TotalScore = 4;
        LevelPalette = SOCCER_PALETTES[Game.Random.Next(0,SOCCER_PALETTES.Length)];
        base._Ready();
        Game.CurrentMode = Mode.GameMode.Soccer;
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
        if(losingTeam) teamDeficit = (float)pointDifference / (TotalScore - 1);
        GD.Print(teamDeficit);

        float comebackScore = GetItemLuck(teamDeficit, GetComebackLuck(player));
        GD.Print(comebackScore);
        Tuple<Item, int>[] items = {
            Tuple.Create(new BigFungus(player) as Item, 10),
            Tuple.Create(new Wings(player) as Item, 9),
            Tuple.Create(new BowlingBall(player) as Item, 8),
            Tuple.Create(new StopSign(player,2) as Item, 7),
            Tuple.Create(new Booll(player) as Item, 5),
            Tuple.Create(new Inverter(player) as Item, 5),
            Tuple.Create(new Pepper(player,2) as Item, 4),
            Tuple.Create(new Moon(player) as Item, 3),
            Tuple.Create(new Ball(player,2) as Item, 2),
            Tuple.Create(new SmallBall(player) as Item, 1)
        };
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        item = SelectWeightedItem(items, probabilities);
        GD.Print(string.Join(",", probabilities));
        return item;
    }
}