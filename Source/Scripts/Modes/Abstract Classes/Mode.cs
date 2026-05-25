using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public abstract partial class Mode : Node{
    protected const float MODE_DEFICIT_MULTIPLIER = 0.75f; //Amount of luck determined by how well you're doing in the mode
    protected const float LUCK_MULTIPLIER = 1-MODE_DEFICIT_MULTIPLIER; //Amount of luck that is purely random
    protected const float COMEBACK_LUCK_MULTIPLIER = 0.5f; //0.25f //Amount of luck determined by how behind you are in tour
    public static Mode ModeNode;
    public static bool Finished;
    public static byte[] Positions;
    public static float[] ItemValues;
    public string Instructions = "";
    protected static byte[] points;
    protected static bool isScoreMode = false;
    public static float[] Scores;
    public static Palette LevelPalette;
    private List<Node2D> cameraTrackedObjects = new List<Node2D>();

    public override void _Ready(){
        ModeNode = this;
        ItemSpawner.TotalSpawners = 0;
        AddChild(Game.CurrentLevel.Instantiate<Level>());
        Game.CurrentLevel = null;
        Finished = false;
        Positions = new byte[Game.MAX_PLAYERS];
        points = new byte[Game.MAX_PLAYERS];
        if(!string.IsNullOrEmpty(Mode.ModeNode.Instructions)) AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        GD.Print("P: " + Game.TotalPlayers);
    }

    public static void GameFinished(){
        if(!Finished){
            ModeNode.SetPoints();
            Finished = true;
            //Game.Players = null;
            if(isScoreMode) ModeNode.Rpc(nameof(ModeNode.ScoreScreenSetUp),points,Positions,Scores);
            else ModeNode.Rpc(nameof(ModeNode.ScoreScreenSetUp),points,Positions);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ScoreScreenSetUp(byte[] points,byte[] positions){
        if(!Finished) Finished = true;
        //Stop any vibrations
        for(int i = 0; i < Game.MAX_PLAYERS; i++) Input.StopJoyVibration(i);
        for(int i = 0; i < positions.Length; i++) Positions[i] = positions[i];
        Tour.GameFinishedPoints(points,positions);
        ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Score Screen/EndBackgroundTransition.tscn").Instantiate<CanvasLayer>());
        if(ModeNode is IRoundEndedEvent roundEnd) roundEnd.OnRoundEnd();
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ScoreScreenSetUp(byte[] points,byte[] positions,float[] scores){
        if(!Finished) Finished = true;
        for(int i = 0; i < positions.Length; i++) Positions[i] = positions[i];
        for(int i = 0; i < Scores.Length; i++) Scores[i] = scores[i];
        Tour.GameFinishedPoints(points,positions);
        ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Score Screen/EndBackgroundTransition.tscn").Instantiate<CanvasLayer>());
    }

    protected abstract void SetPoints();
    public abstract Item GiveItem(Player player);

    //Get the Luck of a Player
    protected static float GetComebackLuck(Player player){
        int playerScore =  Tour.PlayerScores[player.Id-1];
        int maxScore = Tour.PlayerScores[0];
        for(int i = 1; i < Tour.PlayerScores.Length; i++){
            if(Tour.PlayerScores[i] > maxScore) maxScore = Tour.PlayerScores[i];
        }
        int comebackLuck = maxScore - playerScore;//Game.PlayerScores.Max()
        if((Game.TotalPlayers == 2) && comebackLuck == 10) comebackLuck = 5;
        return comebackLuck / (float)Tour.TotalScore;
    }

    protected static float GetItemLuck(float modeDeficit,float playerComebackLuck){
        return (modeDeficit * MODE_DEFICIT_MULTIPLIER) + (playerComebackLuck * COMEBACK_LUCK_MULTIPLIER) + (Game.Random.NextSingle() * LUCK_MULTIPLIER);
    }

    protected static float[] GenerateProbabilities(Tuple<Item, int>[] items, float playerBehindness){
        float[] closeness = new float[items.Length];
        float[] probabilities = new float[closeness.Length];

        for(int i = 0; i < closeness.Length; i++){
            Tuple<Item, int> itemTuple = items[i];
            Item item = itemTuple.Item1;

            // Calculate closeness based on item value and player's behindness
            closeness[i] = MathF.Abs(itemTuple.Item2 - (10 * playerBehindness));
        }

        // Calculate softmax to get probabilities
        float[] expCloseness = closeness.Select(closenessValue => MathF.Exp(-closenessValue)).ToArray();
        float sumExpCloseness = expCloseness.Sum();
        probabilities = expCloseness.Select(expValue => expValue / sumExpCloseness).ToArray();

        return probabilities;
    }

    // Function to select an item based on weighted probabilities
    protected static Item SelectWeightedItem(Tuple<Item, int>[] items, float[] probabilities){
        float randomValue = new Random().NextSingle();
        float cumulativeProbability = 0;

        for(int i = 0; i < items.Length; i++){
            cumulativeProbability += probabilities[i];
            if(randomValue < cumulativeProbability){
                return items[i].Item1;
            }
        }

        return items[items.Length-1].Item1; // Fallback in case of rounding errors
    }

    //Have modes override these if they need specific stuff to occur
    //Player died shouldn't ever be called other than the call in Death.KillPlayer() call that if you want to kill a player
    public virtual void PlayerDied(Player player,Death.DeathCause deathCause){
        player.SpawnPoint = Level.GetRandomRespawn();
    }
    //Gets called when a player disconnects should not ever be called other than in Game.TellClientsWhatToDoAboutDisconnectedPlayerRpc()
    public virtual void PlayerDisconnected(Player player){
        player.Finished = true;
    }

    public virtual void PlayerKilledPlayer(Player playerWhoDied, Player playerWhoKilled, Death.DeathCause deathCause){
        //Keep track of stats
    }
    public virtual void PlayerBumpedPlayer(Player bumper, Player bumped){}
    public virtual void PlayerLaunched(Player player){}
    public virtual void PlayerSlammed(Player player){}
    public virtual void PlayerRespawned(Player player){
        player.Invulnerable = true;
    }
    public virtual string GetPlayerText(Player player){
        return "";
    }
    public virtual float GetChargeMultiplier(Player player){
        return 1;
    }

    public enum GameMode{
        Golf,
        Deathmatch,Survival,
        HotPotato,CrownTheKing,
        Soccer,Volleyball,
        Race,KingOfTheHill,
        BallinToTheBank, Domination,
        Payload, BombBall,
        TargetTest,
        Miscellaneous,None
    }

    public static string EnumToString(GameMode mode){
        switch(mode){
            case GameMode.Race: return "Race";
            case GameMode.Golf: return "Golf";
            case GameMode.KingOfTheHill: return "King of the Hill";
            case GameMode.Deathmatch: return "Deathmatch";
            case GameMode.Soccer: return "Soccer";
            case GameMode.CrownTheKing: return "Crown the King";
            case GameMode.Survival: return "Survival";
            case GameMode.Volleyball: return "Volleyball";
            case GameMode.HotPotato: return "Hot Potato";
            case GameMode.BallinToTheBank: return "Ballin to the Bank";
            case GameMode.Domination: return "Domination";
            case GameMode.Payload: return "Payload";
            case GameMode.BombBall: return "Bomb Ball";
            case GameMode.TargetTest: return "Target Test";
            case GameMode.Miscellaneous: return "Miscellaneous";
            case GameMode.None: return "";
            default: return "Undefined Mode";
        }
    }

    public static string GetModeDescription(GameMode mode){
        switch(mode){
            case GameMode.Race: return "Be the first to win the race";
            case GameMode.Golf: return "Reach the hole with the least strokes";
            case GameMode.KingOfTheHill: return "Stay in the zone for as long as you can";
            case GameMode.Deathmatch: return "Kill your opponents and be the last Ball standing";
            case GameMode.Soccer: return "Score goals on the opposite team";
            case GameMode.CrownTheKing: return "Grab or steal the crown and hold onto it for as long as possible";
            case GameMode.Survival: return "Stay alive as long as possible";
            case GameMode.Volleyball: return "Score points on the opposite team";
            case GameMode.HotPotato: return "Don't get tagged and pass the bomb before the boom";
            case GameMode.BallinToTheBank: return "Collect coins and deposit them into the bank";
            case GameMode.Domination: return "Touch the zones and control them for as long as possible";
            case GameMode.Payload: return "Stay in the zone to push the tower to the objective";
            case GameMode.BombBall: return "Keep the bomb on the opponents side";
            case GameMode.TargetTest: return "Launch into the target zones";
            case GameMode.Miscellaneous: return "Miscellaneous";
            case GameMode.None: return "";
            default: return "Undefined Mode";
        }
    }

    public static List<Node2D> GetCameraTargets(){
        return ModeNode.cameraTrackedObjects;
    }

    public static void AddCameraTarget(Node2D node){
        ModeNode.cameraTrackedObjects.Add(node);
    }

    public static void RemoveCameraTarget(Node2D node){
        ModeNode.cameraTrackedObjects.Remove(node);
    }
}