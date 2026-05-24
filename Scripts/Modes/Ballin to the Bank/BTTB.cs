using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BTTB : Mode, IModeStartEvent{
	public static int MoneyToWin = 40;
	public static int TopBalance;
	//Floating non physics coins 256 can be spawned at most since each needs a unique byte id
	public Dictionary<byte, Coin> SpawnedCoins = new Dictionary<byte, Coin>();
	//Dropped coins from player death (has physics) Only 256 trash can be spawned at once since each needs a unique byte Id
	//But is seperate from normal coins so technically 256 floating coins and 256 dropped coins can both be on screen
    public Dictionary<byte,DroppedCoin> SpawnedDroppedCoins = new Dictionary<byte, DroppedCoin>();
	private static List<CoinSpawner> coinSpawners;
	private readonly static PackedScene DROPPED_COIN_SCENE = GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/Ballin to the Bank/DroppedCoin.tscn");
	private float coinSpawnTimer;
	private Vector2 coinScale;
	private readonly Vector2 MIN_SCALE = new Vector2(0.05f,1);
	private bool growing = false;
	private readonly Palette[] BTTB_PALETTES = new Palette[]{
        new Palette(Color.Color8(255,230,0),Color.Color8(255,196,0),Color.Color8(255,255,0))
    };
	private const float BASE_COIN_SPAWN_TIME = 5;
	private float coinSpawnTime = BASE_COIN_SPAWN_TIME - (2*((float)Game.TotalPlayers/Game.MAX_PLAYERS));
	public static int[] DepositedMoney; //Array of each player's deposited money
	public static int[] HeldPlayerMoney;
	public const float FAST_MUSIC_SPEED = 1.25f;
	private const float ANIMATION_FRAME_TIME = 1f/10f;
	public static readonly Texture2D[] COIN_TEXTURES = new Texture2D[6];
	private float animationTimer = 0;
	public static int AnimationFrame = 0;
	public static Vector2 CoinScale;

    public override void _Ready(){
		LevelPalette = BTTB_PALETTES[Game.Random.Next(0,BTTB_PALETTES.Length)];
		base._Ready();
        Game.CurrentMode = Mode.GameMode.BallinToTheBank;
        Instructions = "Deposit $"+MoneyToWin;
        AddChild(GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/InstructionText.tscn").Instantiate());
        isScoreMode = true;
		coinSpawners  = new List<CoinSpawner>();
		foreach(Node node in Level.LevelNode.GetChildren()){
			if(node is CoinSpawner coinSpawner){
				coinSpawner.Texture = null;
				coinSpawners.Add(coinSpawner);
			}
		}
		coinSpawnTimer = 0;
		coinScale = Vector2.One;
		DepositedMoney = new int[Game.TotalPlayers];
		HeldPlayerMoney = new int[Game.TotalPlayers];
		Scores = new float[Game.TotalPlayers];
		if(COIN_TEXTURES[0] == null){
			for(int i = 0; i < 4; i++){
				COIN_TEXTURES[i] = GD.Load<Texture2D>("res://Assets/Sprites/Mode Stuff/Ballin to the Bank/Coin/"+i+".png");
			}
			COIN_TEXTURES[4] = COIN_TEXTURES[2];
			COIN_TEXTURES[5] = COIN_TEXTURES[1];
		}
		TopBalance = 0;
	}
	
	public override void _Process(double delta){
        animationTimer += (float)delta;
		if(AnimationFrame >= 3){
			CoinScale = new Vector2((1 + ((animationTimer / ANIMATION_FRAME_TIME) / 3)), 1);
		}else if(AnimationFrame != 0){
			CoinScale = new Vector2((1 - ((animationTimer / ANIMATION_FRAME_TIME) / 2)), 1);
		}else{
			CoinScale = new Vector2((1 - ((animationTimer / ANIMATION_FRAME_TIME) / 9)), 1);
		}
		
		if(animationTimer >= ANIMATION_FRAME_TIME){
			animationTimer = 0f;
			if(++AnimationFrame == COIN_TEXTURES.Length) AnimationFrame = 0;
			CoinScale = Vector2.One;
		}
    }

    public void OnModeStart(){
        //Spawn some initial coins
		if(Online.IsHost()){
			for(int i = 0; i < MathF.Ceiling(coinSpawners.Count/3f); i++){
				AttemptCoinSpawn();
			}
		}
    }

    public override void _PhysicsProcess(double delta){
		if(Online.IsHost()){
			coinSpawnTimer += (float)delta;
			if(coinSpawnTimer >= coinSpawnTime){
				AttemptCoinSpawn();
				coinSpawnTimer = 0;
			}
		}
    }

    public override void PlayerDied(Player player, Death.DeathCause deathCause){
		base.PlayerDied(player,deathCause);
		int heldPlayerMoney = HeldPlayerMoney[player.Id-1];
        if(heldPlayerMoney > 0){
			CallDeferred(nameof(RpcSpawnDroppedCoins),player.Rb.GlobalPosition,(byte)heldPlayerMoney);
			HeldPlayerMoney[player.Id-1] = 0; //Setting a value so cant use heldPlayerMoney
		}
    }

    public override void PlayerRespawned(Player player){
		base.PlayerRespawned(player);
		HeldPlayerMoney[player.Id-1] = 0; //So client's money is reset too
    }

	public override string GetPlayerText(Player player){
		return "$"+BTTB.HeldPlayerMoney[player.Id-1];
	}

    public override float GetChargeMultiplier(Player player){
		return 1-( (float)HeldPlayerMoney[player.Id-1] / MoneyToWin * 0.125f );
    }

    private void AttemptCoinSpawn(){
		int spawnAttempts = 0;
		byte coinSpawnerIndex = (byte)Game.Random.Next(0,coinSpawners.Count);
		while(spawnAttempts < coinSpawners.Count){
			if(coinSpawners[coinSpawnerIndex].GetChildCount() == 0){
				CoinSpawner coinSpawner = coinSpawners[coinSpawnerIndex];
				Vector2 spawnPosition = coinSpawner.GlobalPosition;
				CoinSpawner.CoinPattern pattern = coinSpawner.CoinPatterns[Game.Random.Next(0,coinSpawner.CoinPatterns.Count)];
				RpcSpawnCoinPattern(coinSpawnerIndex, (byte)pattern);
				break;
			}else{
				spawnAttempts++;
				coinSpawnerIndex++;
				if(coinSpawnerIndex > coinSpawners.Count-1) coinSpawnerIndex = 0;
			}
		}
	}

    public static void RpcSpawnCoinPattern(byte coinSpawnerIndex, byte coinPatternEnum){
		BTTB modeNode = Mode.ModeNode as BTTB;
		modeNode.Rpc(nameof(modeNode.SpawnCoinPatternRpc),coinSpawnerIndex,coinPatternEnum);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SpawnCoinPatternRpc(byte coinSpawnerIndex, byte coinPatternEnum){
		int coinPatternIndex = CoinSpawner.EnumToIndex((CoinSpawner.CoinPattern)coinPatternEnum);
		Node coinPattern = CoinSpawner.COIN_PATTERNS[coinPatternIndex].Instantiate();
		Godot.Collections.Array<Node> coinsToSpawn = coinPattern.GetChildren();
		foreach(Node node in coinsToSpawn){
			if(node is Coin coinTemplate){
				Coin newCoin = GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/Ballin to the Bank/Coin.tscn").Instantiate<Coin>();
				//Set Id
				HashSet<byte> keys = new HashSet<byte>(SpawnedCoins.Keys);
        		byte? newCoinId = ItemSynchronizer.GetUnusedItemId(keys);
        		if(newCoinId != null){
					newCoin.Id = (byte)newCoinId;
					SpawnedCoins.Add(newCoin.Id,newCoin);
        		}else{ //Too many coins spawned delete oldest to make room
        		    byte firstCoinId = SpawnedCoins.ElementAt(0).Key; //Get oldest coin id
					Coin oldCoinToDelete = SpawnedCoins[firstCoinId]; //Get oldest coin
					SpawnedCoins.Remove(firstCoinId); //Remove oldest coin from dictionary
					oldCoinToDelete.QueueFree(); //Delete oldest coin
					newCoin.Id = firstCoinId; //Set new coins id to the old coin id
					SpawnedCoins.Add(newCoin.Id,newCoin); //Spawn new coin
        		}
				//Set Position
				newCoin.Position = coinTemplate.Position;
				//Spawn it
				coinSpawners[coinSpawnerIndex].AddChild(newCoin);
			}
		}
		coinPattern.Free(); //Prevent memory leak
	}

	public static void RpcCoinCollected(byte coinId, byte playerId){
		BTTB modeNode = (BTTB)Mode.ModeNode;
		modeNode.Rpc(nameof(modeNode.CoinCollectedRpc),coinId,playerId);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void CoinCollectedRpc(byte coinId,byte playerId){
		SFX.Play("Coin");
		Player player = Game.Players[playerId-1];
		HeldPlayerMoney[player.Id-1]++;
		player.ShowPlayerText();
		Coin coin = SpawnedCoins[coinId];
        SpawnedCoins.Remove(coinId);
        coin.QueueFree();
	}

	public void RpcSpawnDroppedCoins(Vector2 position, byte droppedCoinCount){
		Rpc(nameof(SpawnDroppedCoinsRpc),position,droppedCoinCount);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SpawnDroppedCoinsRpc(Vector2 position, byte droppedCoinCount){
		Random droppedCoinRandom = new Random((int)(position.X*position.Y));
		for(int i = 0; i < droppedCoinCount; i++){
			DroppedCoin droppedCoin = DROPPED_COIN_SCENE.Instantiate<DroppedCoin>();
			droppedCoin.LifeTimer = 6.5f + (1*droppedCoinRandom.NextSingle());
			HashSet<byte> keys = new HashSet<byte>(SpawnedDroppedCoins.Keys);
        	byte? newCoinId = ItemSynchronizer.GetUnusedItemId(keys);
        	if(newCoinId != null){
				droppedCoin.Id = (byte)newCoinId;
				SpawnedDroppedCoins.Add(droppedCoin.Id,droppedCoin);
        	}else{ //Too many coins spawned delete oldest to make room
        	    byte firstCoinId = SpawnedDroppedCoins.ElementAt(0).Key; //Get oldest coin id
				DroppedCoin oldCoinToDelete = SpawnedDroppedCoins[firstCoinId]; //Get oldest coin
				SpawnedDroppedCoins.Remove(firstCoinId); //Remove oldest coin from dictionary
				oldCoinToDelete.QueueFree(); //Delete oldest coin
				droppedCoin.Id = firstCoinId; //Set new coins id to the old coin id
				SpawnedDroppedCoins.Add(droppedCoin.Id,droppedCoin); //Spawn new coin
        	}
			//Set Position
			if(droppedCoin.Rb == null) droppedCoin.Rb = droppedCoin.GetNode<InterpolatedBody>("RigidBody2D");
			//Call teleport
			droppedCoin.Rb.SetDeferred("global_position", position);
			droppedCoin.Rb.SkipInterpolation();
			droppedCoin.Rb.LinearVelocity = Vector2.FromAngle(droppedCoinRandom.NextSingle()*MathF.PI + MathF.PI) * droppedCoinRandom.NextSingle()*(droppedCoinCount/50f * 2000 + 1500);
			//Spawn it
			Level.LevelNode.AddChild(droppedCoin);
		}
	}

	public static void RpcDroppedCoinCollected(byte coinId, byte playerId){
		BTTB modeNode = (BTTB)Mode.ModeNode;
		modeNode.Rpc(nameof(modeNode.DroppedCoinCollectedRpc),coinId,playerId);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void DroppedCoinCollectedRpc(byte coinId,byte playerId){
		SFX.Play("Coin");
		Player player = Game.Players[playerId-1];
		HeldPlayerMoney[player.Id-1]++;
		player.ShowPlayerText();
		RemoveDroppedCoin(coinId);
	}
	public static void RemoveDroppedCoin(byte coinId){
		BTTB modeNode = (BTTB)Mode.ModeNode;
		DroppedCoin coin = modeNode.SpawnedDroppedCoins[coinId];
        modeNode.SpawnedDroppedCoins.Remove(coinId);
        coin.QueueFree();
	}

	protected override void SetPoints(){
		int[] sortedScores = new int[Game.TotalPlayers];
        // Populate sortedScores
        for(int i = 0; i < DepositedMoney.Length; i++){
            sortedScores[i] = DepositedMoney[i];
        }
    
        GD.Print("BTTB Scores: " + string.Join(",", DepositedMoney));
    
        Array.Sort(sortedScores);
        Array.Reverse(sortedScores);
        GD.Print("BTTB Sorted Scores: " + string.Join(",", sortedScores));
    
		for(int i = 0; i < Game.TotalPlayers; i++){
			Positions[i] = (byte)(Array.IndexOf(sortedScores, DepositedMoney[i]) + 1);
		}
    }

	public override Item GiveItem(Player player){
		foreach(int balance in DepositedMoney){
			if(balance > TopBalance) TopBalance = balance;
		}

        Tuple<Item, int>[] items = {
            Tuple.Create((Item)new Booll(player), 12),
            Tuple.Create((Item)new BigFungus(player), 10),
            Tuple.Create((Item)new Wings(player), 9),
            Tuple.Create((Item)new Moon(player), 7),
            Tuple.Create((Item)new StopSign(player,2), 6),
            Tuple.Create((Item)new Pepper(player,2), 5),
            Tuple.Create((Item)new Inverter(player), 5),
			Tuple.Create((Item)new StopSign(player,1), 4),
			Tuple.Create((Item)new BowlingBall(player), 3),
			Tuple.Create((Item)new Pepper(player,2), 3),
			Tuple.Create((Item)new Ball(player,3), 3),
			Tuple.Create((Item)new Ball(player,2), 2),
			Tuple.Create((Item)new Ball(player,1), 1),
			Tuple.Create((Item)new SmallBall(player), 1),
        };

        float maxScoreThreshold = TopBalance;
        float deficit = 1 - (BTTB.DepositedMoney[player.Id-1]/maxScoreThreshold);
        
        float comebackScore = GetItemLuck(deficit,GetComebackLuck(player));
        // Generate weighted probabilities
        float[] probabilities = GenerateProbabilities(items, comebackScore);
        // Select an item based on weighted probabilities
        return SelectWeightedItem(items, probabilities);
	}
}