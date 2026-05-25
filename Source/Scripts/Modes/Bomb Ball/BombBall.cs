using Godot;
using System;

public partial class BombBall : TeamSportsMode{
    private readonly Palette[] BOMB_BALL_PALETTES = new Palette[]{
        new Palette(Color.Color8(207,186,225),Color.Color8(197,159,201),Color.Color8(133,0,249))
    };
    
    public override void _Ready(){
        LevelPalette = BOMB_BALL_PALETTES[Game.Random.Next(0,BOMB_BALL_PALETTES.Length)];
        TotalScore = 3;
        Game.CurrentMode = Mode.GameMode.BombBall;
        Instructions = "Get the Bomb off your side";
        base._Ready();
    }

    public override void OnModeStart(){
        base.OnModeStart();
        SetBombTimers();
    }

    protected override void OnPointScored(string teamScoredOn){
        SFX.Play("Explosion");
        TheBomb bomb = TeamSportsMode.SportBall as TheBomb;
        SetBombTimers();
        bomb.BombTimerText.Text = "";
        bomb.BombTimerText.SelfModulate = Game.CLEAR;
        bomb.Rb.GlobalRotation = 0;
        bomb.Rb.LinearVelocity = Vector2.Zero;
    }
    
    public override void SetStartingScores(){
        TeamAScore = 0;
        TeamBScore = 0;
    }

    public static void SetBombTimers(){
        TheBomb bomb = TeamSportsMode.SportBall as TheBomb;
        const float SOLO_BONUS = 5;
        const float ONE_LESS_BONUS = 2.5f;
        if(TeamAPlayerCount == TeamBPlayerCount){
            bomb.TeamABombTimer = TheBomb.BOMB_TIME;
            bomb.TeamBBombTimer = TheBomb.BOMB_TIME;
        }else if(TeamAPlayerCount > TeamBPlayerCount){
            bomb.TeamABombTimer = TheBomb.BOMB_TIME - (TeamBPlayerCount == 1 ? SOLO_BONUS : ONE_LESS_BONUS);
            bomb.TeamBBombTimer = TheBomb.BOMB_TIME;
        }else{
            bomb.TeamABombTimer = TheBomb.BOMB_TIME;
            bomb.TeamBBombTimer = TheBomb.BOMB_TIME - (TeamAPlayerCount == 1 ? SOLO_BONUS : ONE_LESS_BONUS);
        }
    }

    public override Item GiveItem(Player player){
        Item item;
        int pointDifference;
        bool losingTeam;
        switch (player.Team){
            case "A":
                losingTeam = TeamAScore < TeamBScore;
                break;
            default://case "B":
                losingTeam = TeamAScore > TeamBScore;
                break;
        }

        pointDifference = (int)MathF.Abs(TeamAScore - TeamBScore);

        float teamDeficit = 0;
        if (losingTeam) teamDeficit = (float)pointDifference / (TotalScore - 1);
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