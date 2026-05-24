using Godot;
using System.Collections.Generic;

//Ensures all players are connected and have loaded the Level before starting the round
public partial class OnlineReadier : Node{
	private List<int> loadedInPlayers = new List<int>();
	private bool sentReady = false; //Client
	private bool startedRound = false; //Both
	private bool spawnedPlayers = false;
	private List<int> clientsWithPlayersSpawned = new List<int>();
	private Label statusText;
	private float kickTimer = 0;
	private const float KICK_TIMEOUT = 3;
	//Menu Background
	private CanvasLayer menuBackgroundLayer;
	private Node2D background;

    public async override void _Ready(){
		SetProcess(false);
		Multiplayer.PeerDisconnected += PlayerDisconnected;
        statusText = GetNode<Label>("CanvasLayer/StatusText");
		if(Online.IsOnline) statusText.Text = "Waiting for players";
		GetNode<CanvasLayer>("CanvasLayer").Scale = Game.ContentScaleVector2;
		menuBackgroundLayer = Game.GameNode.GetNodeOrNull<CanvasLayer>("BackgroundLayer");
		if(menuBackgroundLayer != null){
			background = menuBackgroundLayer.GetNode<Node2D>("Background");
		}
		UnreliableManager.ResetTransferChannelLastUpdate();
		UnreliableManager.ResetClientChannelLastUpdate();
		UnreliableManager.ResetHostClientChannelLastUpdate();
		//Wait for level to load before continuing
		Level levelNode = null;
		while(levelNode == null){
			await ToSignal(GetTree().CreateTimer(0.1f,true,false,true), "timeout");
			levelNode = GetParent().GetNodeOrNull<Level>("Level");
		}

		if(Online.IsOnline){
			//Tell host we are ready and loaded in
			if(Online.IsHost()) ClientReady();
			else RpcId(1,nameof(ClientReady));
		}else{
			//Offline so start right away
			statusText.Visible = false;
			levelNode.HostSpawnPlayers();
			StartRound();
			QueueFree();
		}
		if(Online.IsHost()) statusText.Text += loadedInPlayers.Count + "/" + Game.TotalPlayers;
		SetProcess(true);
    }

    public override void _Process(double delta){
		float fDelta = (float)delta;
		if(!startedRound){
			//Client send ready message
			if(Online.IsHost()){
				if(!spawnedPlayers){
					if(loadedInPlayers.Count >= getExpectedPlayerCount() && !spawnedPlayers){
						spawnedPlayers = true;
						Level levelNode = GetParent().GetNode<Level>("Level");
						//This Rpcs the spawning player data to all clients
						//So now we have to wait for all clients to have spawned the players
						levelNode.HostSpawnPlayers();
						kickTimer = 0;
					}else{
						kickPlayerCheck(loadedInPlayers);
					}
				}else{
					if(!startedRound){
						if(clientsWithPlayersSpawned.Count >= getExpectedPlayerCount()){
							Rpc(nameof(StartRound));
							kickTimer = 0;
						}
					}else{
						kickPlayerCheck(clientsWithPlayersSpawned);
					}
				}

				void kickPlayerCheck(List<int> playerList){
					kickTimer += fDelta;
					if(kickTimer >= KICK_TIMEOUT){
						foreach(PlayerData playerInfo in Game.PlayerDatas){
							if(!playerList.Contains(playerInfo.UUID)){
								Online.KickPlayer(playerInfo.UUID);
							}
						}
					}
				}
				
				int getExpectedPlayerCount(){
					return Online.IsOnline ? Game.PlayerDatas.Count + Game.SpectatorDatas.Count - Game.DisconnectedDatas.Count : 1;
				}
			}
		//Auto delete this node after 5 seconds so there are no Rpc errors
		}else{
			MenuBackgroundFadeout(fDelta); //Online fadeout
			if(!Online.IsOnline) QueueFree();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StartRound(){
		GetTree().Paused = false;
		if(statusText != null) statusText.Visible = false;
		startedRound = true;
		foreach(Player player in Game.Players) player.Visible = true;
		if(Game.CurrentMode == Mode.GameMode.Race) Engine.TimeScale = Countdown.RACE_START_TIMESCALE;
		SFX.Play("Whip");
		if(!Online.IsOnline){
			Tween tween = Game.GameNode.CreateTween();
        	tween.TweenProperty(background,"self_modulate",Game.CLEAR,0.75 * (Game.CurrentMode == Mode.GameMode.Race ? Countdown.RACE_START_TIMESCALE : 1));
			tween.TweenCallback(Callable.From(menuBackgroundLayer.QueueFree));
		}
		//Mode.ModeNode.OnModeStart();
		if(Mode.ModeNode is IModeStartEvent modeStart) modeStart.OnModeStart();
	}
	//Rpc Id
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ClientReady(){
		if(Online.IsHost()){
			int senderUUID = Game.GameNode.Multiplayer.GetRemoteSenderId();
			loadedInPlayers.Add(senderUUID);
			if(statusText != null)
				statusText.Text = "Waiting for players to load in: " + (loadedInPlayers.Count) + "/" + Game.TotalPlayers;
		}else{
			GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ClientSpawnedPlayers(){
		if(Online.IsHost()){
			clientsWithPlayersSpawned.Add(Game.GameNode.Multiplayer.GetRemoteSenderId());
			if(statusText != null)
				statusText.Text = "Waiting for players to spawn in: " + (clientsWithPlayersSpawned.Count) + "/" + Game.TotalPlayers;
		}else{
			GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
		}
	}

	private void PlayerDisconnected(long id){
		if(!startedRound){
			for(int i = 0; i < Game.PlayerDatas.Count; i++){
				if(Game.PlayerDatas[i].UUID == id){
					if(loadedInPlayers.Contains((int)id)) loadedInPlayers.Remove((int)id);
					if(clientsWithPlayersSpawned.Contains((int)id)) clientsWithPlayersSpawned.Remove((int)id);
					if(statusText != null && Online.IsHost() && !spawnedPlayers) statusText.Text = "Waiting for Players: " + (loadedInPlayers.Count) + "/" + Game.PlayerDatas.Count;
					else if(statusText != null && Online.IsHost()) statusText.Text = "Waiting for Players: " + (clientsWithPlayersSpawned.Count) + "/" + Game.PlayerDatas.Count;
				}
			}
		}
	}

	private void MenuBackgroundFadeout(float delta){
		if(menuBackgroundLayer != null){
			float alpha = background.SelfModulate.A;
			alpha -= delta*0.75f/(float)Engine.TimeScale;
			if(alpha <= 0){
				menuBackgroundLayer.QueueFree();
				menuBackgroundLayer = null;
				QueueFree();
			}else{
				background.SelfModulate = new Color(background.SelfModulate,alpha);
			}
		}
	}
}