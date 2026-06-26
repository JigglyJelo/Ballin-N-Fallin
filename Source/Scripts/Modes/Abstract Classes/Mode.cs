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
    /// <summary>The End Results (Positions) Each Player got in the Mode</summary>
    public static byte[] Positions;
    public static float[] ItemValues;
    public string Instructions = "";
    protected static byte[] points;
    protected static bool isScoreMode = false;
    public static float[] Scores;
    public static Palette LevelPalette;
    /// <summary>Keeps track of Objects the camera will keep on screen</summary>
    private List<Node2D> cameraTrackedObjects = new List<Node2D>();

    public override void _Ready(){
        ModeNode = this;
        ItemSpawner.TotalSpawners = 0;
        AddChild(Game.CurrentLevel.Instantiate<Level>());
        Game.CurrentLevel = null;
        Finished = false;
        Positions = new byte[Game.MAX_PLAYERS];
        points = new byte[Game.MAX_PLAYERS];
        if(!string.IsNullOrEmpty(Mode.ModeNode.Instructions)) AddChild(GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        GD.Print("P: " + Game.TotalPlayers);
    }

    /// <summary>Call to end the current Mode/Round</summary>
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
        ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Score Screen/EndBackgroundTransition.tscn").Instantiate<CanvasLayer>());
        if(ModeNode is IRoundEndedEvent roundEnd) roundEnd.OnRoundEnd();
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ScoreScreenSetUp(byte[] points,byte[] positions,float[] scores){
        if(!Finished) Finished = true;
        for(int i = 0; i < positions.Length; i++) Positions[i] = positions[i];
        for(int i = 0; i < Scores.Length; i++) Scores[i] = scores[i];
        Tour.GameFinishedPoints(points,positions);
        ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Score Screen/EndBackgroundTransition.tscn").Instantiate<CanvasLayer>());
    }
    /// <summary>Distributes points to players at the end of the round based on their performance</summary>
    protected abstract void SetPoints();
    /// <summary>Determines which specific item to grant a player</summary>
    /// <param name="player">The player receiving the item.</param>
    /// <returns>The generated item based on the current game state and player standings.</returns>
    public abstract Item GiveItem(Player player);

    /// <summary>
    /// Calculates a Comeback multiplier based on how many points behind the player is from the tour's current leader
    /// </summary>
    /// <param name="player">The player whose luck is being calculated.</param>
    /// <returns>A float representing the luck ratio, normalized against the total points needed to win.</returns>
    /// <remarks>
    /// Includes a specific edge-case adjustment for 2-player games where a 10-point deficit is halved.
    /// </remarks>
    protected static float GetComebackLuck(Player player){
        int playerScore =  Tour.PlayerScores[player.Id-1];
        int maxScore = Tour.PlayerScores[0];
        for(int i = 1; i < Tour.PlayerScores.Length; i++){
            if(Tour.PlayerScores[i] > maxScore) maxScore = Tour.PlayerScores[i];
        }
        int comebackLuck = maxScore - playerScore;//Game.PlayerScores.Max()
        if((Game.TotalPlayers == 2) && comebackLuck == 10) comebackLuck = 5;
        return comebackLuck / (float)Tour.CurrentTour.PointsToWin;
    }

    /// <summary>
    /// Calculates the final luck value for item generation by combining the mode deficit, the player's comeback luck, and a randomized factor.
    /// </summary>
    /// <param name="modeDeficit">The baseline difficulty or deficit for the current game mode.</param>
    /// <param name="playerComebackLuck">The calculated catch-up value for the specific player.</param>
    /// <returns>The combined, weighted luck value.</returns>
    protected static float GetItemLuck(float modeDeficit,float playerComebackLuck){
        return (modeDeficit * MODE_DEFICIT_MULTIPLIER) + (playerComebackLuck * COMEBACK_LUCK_MULTIPLIER) + (Game.Random.NextSingle() * LUCK_MULTIPLIER);
    }

    /// <summary>
    /// Generates a normalized array of probabilities (using a Softmax function) to determine how likely each item is to be drawn.
    /// </summary>
    /// <param name="items">An array of tuples where Item1 is the Item, and Item2 is its base integer value/weight.</param>
    /// <param name="playerBehindness">A float representing how far behind the player is, used to scale the target item value.</param>
    /// <returns>An array of floats summing to 1.0, representing the probability of selecting the item at the corresponding index.</returns>
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

    /// <summary>
    /// Performs a weighted random selection (roulette wheel selection) to pick an item based on the provided probabilities.
    /// </summary>
    /// <param name="items">The pool of available items.</param>
    /// <param name="probabilities">An array of probability weights corresponding to the items array. Must sum to 1.0.</param>
    /// <returns>The selected item.</returns>
    /// <remarks>
    /// Will return the last item in the array as a safe fallback if floating-point rounding errors prevent a selection.
    /// </remarks>
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
    /// <summary>Gets called when a player dies</summary>
    /// <remarks>Shouldn't ever be called other than the call in Death.KillPlayer() call that if you want to kill a player</remarks>
    public virtual void PlayerDied(Player player,Death.DeathCause deathCause){
        player.SpawnPoint = Level.GetRandomRespawn();
    }

    /// <summary>Gets called when a player disconnects</summary>
    /// <remarks>Shouldn't ever be called other than in Game.TellClientsWhatToDoAboutDisconnectedPlayerRpc()</remarks>
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
    public virtual void OnPlayerEnterRegain(Player player){
        player.CanLaunch = true;
        player.CanSlam = true;
    }
    public virtual void PlayerRegainCheck(Player player, float delta){
        if(player.IsRegaining){
            player.CanLaunch = true;
            player.CanSlam = true;
        }
    }
    public virtual float MoonAirTimeRequirement => 0.75f;
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