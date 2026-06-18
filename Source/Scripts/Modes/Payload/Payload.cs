using Godot;
using System;

public partial class Payload : TeamMode, ILevelLoadedEvent{
    private readonly Palette[] PAYLOAD_PALETTES = new Palette[]{
        new Palette(Color.Color8(253,152,63),Color.Color8(214,81,8),Color.Color8(89,31,10))
        //new Palette(Color.Color8(243,201,139),Color.Color8(218,165,136),Color.Color8(196,109,94))
    };

    public static PayloadTower payload;
    public static Path2D PayloadPath;

	public override void _Ready(){
        LevelPalette = PAYLOAD_PALETTES[Game.Random.Next(0,PAYLOAD_PALETTES.Length)];
        Instructions = "Control the Zone";
        base._Ready();
        Game.CurrentMode = Mode.GameMode.Payload;
        isScoreMode = false;
	}

    public void OnLevelLoaded(){
        payload = GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Payload/Payload.tscn").Instantiate<PayloadTower>();
        Level.LevelNode.AddChild(payload);
    }

    public override Item GiveItem(Player player){
        Item item;
        float teamDeficit = 0.5f;
        switch(player.Team){
            case "A":
                teamDeficit = 1 - payload.Distance;
                break;
            case "B":
                teamDeficit = payload.Distance;
                break;
        }
        float comebackScore = GetItemLuck(teamDeficit,GetComebackLuck(player));
        GD.Print(comebackScore);
        Tuple<Item, int>[] items = {
            Tuple.Create(new Booll(player) as Item, 12),
            Tuple.Create(new BigFungus(player) as Item, 10),
            Tuple.Create(new Wings(player) as Item, 9),
            Tuple.Create(new BowlingBall(player) as Item, 8),
            Tuple.Create(new Moon(player) as Item, 5),
            Tuple.Create(new StopSign(player,2) as Item, 4),
            Tuple.Create(new Pepper(player,2) as Item, 4),
            Tuple.Create(new Ball(player,2) as Item, 3),
            Tuple.Create(new Inverter(player) as Item, 2),
            Tuple.Create(new SmallBall(player) as Item, 1)
        };
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        item = SelectWeightedItem(items, probabilities);
        GD.Print(string.Join(",",probabilities));
        return item;
    }
}