using Godot;

public partial class SceneTransitioner : Node{
	public static SceneTransitioner SceneTransNode;
	private static PackedScene gameScene,menuScene,scoreScene;
	public override void _Ready(){
		Game.DisableProcesses(this);
		SceneTransNode = this;
		gameScene = GD.Load<PackedScene>("res://Source/Scenes/Main Scenes/Game Scene.tscn");
        menuScene = GD.Load<PackedScene>("res://Source/Scenes/Main Scenes/Menu Scene.tscn");
        scoreScene = GD.Load<PackedScene>("res://Source/Scenes/Main Scenes/Score Scene.tscn");
	}

	//Switches to the given scene "Game" for Game Scene "Menu" for Menu Scene
    public static void SwitchToScene(Game.SceneType scene){
        SwitchToScene(scene,true);
    }
    public static void SwitchToScene(Game.SceneType scene, bool free){
        //Game.ClearFontCache(); //Breaks exported game
		//Reset music speed and pitch
		Game.CurrentScene = scene;
		MusicPlayer.MusicNode = null;
		MusicPlayer.SetPitch(1);
		//Load next scene
        PackedScene newPackedScene;
		MenuScene.CurrentMenuNode = null;
		switch(scene){
			case Game.SceneType.Game:
				newPackedScene = gameScene;
            	if(Game.MouseMode != Game.MouseModeEnum.Cursor) Input.MouseMode = Input.MouseModeEnum.Hidden;
				break;
			case Game.SceneType.ScoreScreen:
				//Set background new size
            	Game.Players = null;
            	Game.GameNode.GetNodeOrNull<CanvasLayer>("BackgroundLayer").Scale = new Vector2(Game.Resolution / 2160f,Game.Resolution / 2160f);
            	newPackedScene = scoreScene;
            	Input.MouseMode = Input.MouseModeEnum.Visible;
				break;
			default:
				Game.Players = null;
            	newPackedScene = menuScene;
            	Input.MouseMode = Input.MouseModeEnum.Visible;
				break;
		}
        Node currentScene = Game.GameNode.GetNodeOrNull("Scene");
        if(currentScene != null){
            currentScene.Name = "OldSceneWhichIsAboutToGetDeleted";
            if(free){
		        currentScene.QueueFree();
            }
        }
        
		
        Node newScene = newPackedScene.Instantiate();
        newScene.Name = "Scene";
        Game.GameNode.AddChild(newScene);
        //Hack needed so there isn't a one frame flicker with the Background transition to score screen
        if(scene == Game.SceneType.ScoreScreen){
            if(currentScene != null) currentScene.Free();
        }
        Game.Paused = false;
        PrintOrphanNodes();
	}

	public static void RpcStartNewTourFromScoreScreen(){
		Tour.PrepareTour();
		SceneTransNode.Rpc(nameof(StartNewTourFromScoreScreenRpc),(byte)Game.CurrentMode,Game.CurrentLevelName,Game.CurrentFolderPath);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StartNewTourFromScoreScreenRpc(byte nextMode,string nextLevel,string nextFolderPath){
		Tour.PlayerScores = new int[Tour.PlayerScores.Length];
		SceneTransNode.StartNextRoundRpc(nextMode,nextLevel,nextFolderPath);
	}

	public static void RpcStartNextRound(byte nextMode,string nextLevel,string nextFolderPath){
		SceneTransNode.Rpc(nameof(SceneTransNode.StartNextRoundRpc),nextMode,nextLevel,nextFolderPath);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StartNextRoundRpc(byte nextMode,string nextLevel,string nextFolderPath){
		//Start menu background fadeout
		CanvasLayer backgroundLayer = Game.GameNode.GetNodeOrNull<CanvasLayer>("BackgroundLayer");
		if(backgroundLayer != null){
			MenuBackground background = backgroundLayer.GetNode<MenuBackground>("Background");
			background.GlobalPosition = Vector2.Zero;
			backgroundLayer.Layer = 1;
			backgroundLayer.FollowViewportEnabled = false;
			backgroundLayer.Reparent(Game.GameNode);
		}

		//Setup next round
		Mode.GameMode nextGameMode = (Mode.GameMode)nextMode;
		Game.CurrentMode = nextGameMode;
		Game.CurrentLevelName = nextLevel;
		Game.CurrentFolderPath = nextFolderPath;
		GetTree().Paused = false;
		if(Online.IsOnline) Game.TotalPlayers = Game.PlayerDatas.Count;
		Game.SetLevel(nextGameMode,Game.CurrentLevelName,Game.CurrentFolderPath);
		Online.RemoveDisconnectedPlayerInfos();
		SwitchToScene(Game.SceneType.Game);
	}

	public static void RpcReturnToLobby(){
		SceneTransNode.Rpc(nameof(SceneTransNode.ReturnToLobbyRpc));
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ReturnToLobbyRpc(){
		Game.Paused = false;
		GetTree().Paused = false;
		Engine.TimeScale = 1;
		Online.RemoveDisconnectedPlayerInfos();
		if(Online.IsOnline){
			Game.TotalPlayers = Game.PlayerDatas.Count;
			MenuScene.MenuToLoad = "Online/OnlineLobby";
		}else if(Game.TotalPlayers == 1){
			MenuScene.MenuToLoad = "SoloMenu";
		}else if(!Tour.IsTour){
			MenuScene.MenuToLoad = "ModeMenu";
		}else{
			MenuScene.MenuToLoad = "TourMenu";
		}
		CanvasLayer backgroundLayer = Game.GameNode.GetNodeOrNull<CanvasLayer>("BackgroundLayer");
		if(backgroundLayer != null){
			MenuBackground.StartPoint = backgroundLayer.GetNode<Polygon2D>("Background").TextureOffset;
			backgroundLayer.QueueFree();
		} 
		SwitchToScene(Game.SceneType.Menu);
	}
}