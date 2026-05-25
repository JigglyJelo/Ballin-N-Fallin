using System.Collections.Generic;
using Godot;

public partial class PauseMenu : VerticalMenu{
	public static byte Pauser;
	private Label pausedLabel,resumeLabel,quitLabel;
	private double timeScale;
	//Online
	private float playerTextTimer = 0;
	private const float UPDATE_PLAYER_TEXTS_TIME = 0.125f;
	private List<OnlinePlayerText> playerTexts = new List<OnlinePlayerText>();
	private Sprite2D banButton;

	public override void _Ready(){

		if(Online.IsHost() && Online.IsOnline){
			GetNode("Selections").Free();
			Node selectionsNode = GetNode("SelectionsOnline");
			selectionsNode.Name = "Selections";
			totalSelections = 3;
		}else{
			GetNode("SelectionsOnline").QueueFree();
			totalSelections = 2;
		}

		timeScale = Engine.TimeScale;
		Engine.TimeScale = 1;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Selection = 1;
		defaultFontSize = 1;
		Game.Paused = true;
		GetParent<CanvasLayer>().Scale = Game.ContentScaleVector2;
		pausedLabel = GetNode<Label>("PausedText");
		resumeLabel = GetNode<Label>("Selections/ResumeText");
		quitLabel = GetNode<Label>("Selections/QuitText");
		pausedLabel.SelfModulate = Game.PlayerDatas[Pauser-1].PlayerColor; //Online.IsOnline ? Online.PlayerColors[Pauser-1] : Game.PlayerColors[Game.InputIds[Pauser-1]-1];
		UpdateSelectionVisual();
		
		if(!Online.IsOnline){
			//Disable any current Vibrations
			for(int i = 0; i < Game.MAX_PLAYERS; i++) Input.StopJoyVibration(i);
			MusicPlayer.PauseMusic(true);
			GetTree().Paused = true;
			GetNode<Sprite2D>("BanButton").QueueFree();
		}else{
			banButton = GetNode<Sprite2D>("BanButton");
			//Create Player Ping Texts
			PackedScene playerTextScene = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlinePlayerText.tscn");
			for(int i = 0; i < Game.PlayerDatas.Count; i++){
				OnlinePlayerText playerText = playerTextScene.Instantiate<OnlinePlayerText>();
				playerText.Name = "Player " + (i+1) + " Text";
				playerText.Position = new Vector2(2432,(200*i)+500);
				playerText.Id = i;
				playerText.UUID = Game.PlayerDatas[i].UUID;
				playerText.Visible = true;
				playerTexts.Add(playerText);
				AddChild(playerText);
			}
		}
	}

	public override void _Process(double delta){
		int inputId = (int)Game.PlayerDatas[Pauser-1].InputDevice; //Online.IsOnline ? Online.PlayerInfos[Pauser-1].InputId : Game.InputIds[Pauser-1];
		InputChecks(delta,inputId);
		//Toggle Vibration
		for(int i = 0; i < Game.TotalPlayers; i++){
			if(Input.IsActionJustReleased("Y" + (i+1))){
				Game.PlayerDatas[i].VibrationEnabled = !Game.PlayerDatas[i].VibrationEnabled;
            	if(Game.PlayerDatas[i].VibrationEnabled) Input.StartJoyVibration(i,1,1,0.25f);
        	}
		}

		if(Online.PeerIsActive()){
			playerTextTimer += (float)delta;
			if(playerTextTimer >= UPDATE_PLAYER_TEXTS_TIME){
				UpdatePlayerTexts();
				playerTextTimer = 0;
			}
			if(Online.IsHost()){
				KickBanPlayerButtons();
			}
		}
	}

	private void UpdatePlayerTexts(){
		bool reset = false;
		for(int i = 0; i < Game.DisconnectedDatas.Count; i++){
			for(int j = 0; j < playerTexts.Count; j++){
				if(playerTexts[j].UUID == Game.DisconnectedDatas[i].UUID){
					playerTexts[j].QueueFree();
					playerTexts.RemoveAt(j);
					reset = true;
					//(Not yet implemented) Replace dc'd players player text with a grayed out version indicating a dc
					break;
				}
			}
		}
		if(reset){
			for(int i = 0; i < playerTexts.Count; i++){
				OnlinePlayerText playerText = playerTexts[i];
				playerText.ResetPlayerText();
				playerText.Position = new Vector2(2432,(200*i)+500);
			}
		}
	}

	private void KickBanPlayerButtons(){
		Vector2 globalMousePosition = banButton.GetGlobalMousePosition();
        if(globalMousePosition.X > 192 + 2432 && globalMousePosition.X < 3840){
			//Check each player text to see if any are hovered over
			for(int i = 0; i < playerTexts.Count; i++){
				//Player text is hovered
				if(globalMousePosition.Y > playerTexts[i].GlobalPosition.Y && globalMousePosition.Y < playerTexts[i].GlobalPosition.Y + 128){
					banButton.GlobalPosition = new Vector2(banButton.GlobalPosition.X, playerTexts[i].GlobalPosition.Y);
					banButton.Visible = true;
					//Ban button hovered over
					if(globalMousePosition.X > banButton.GlobalPosition.X && globalMousePosition.X < banButton.GlobalPosition.X + 128){
						banButton.SelfModulate = new Color(0.8f,0,0);
						//Click button
						if(Input.IsActionJustReleased("Charge N Launch Mouse")){
							Online.BanPlayer(playerTexts[i].UUID);
							//GD.Print("Banned " + Online.PlayerInfos[i].Username);
						}else if(Input.IsActionJustReleased("Slam Mouse")){
							Online.KickPlayer(playerTexts[i].UUID);
							//GD.Print("Kicked " + Online.PlayerInfos[i].Username);
						}
					}else{
						banButton.SelfModulate = new Color(1,0,0);
					}
					break;
				}
				//None are hovered hide ban button
				banButton.Visible = false;
			}
		}else{
			banButton.Visible = false;
		}
	}

	protected override void MenuChoose(int choice){
		if(Online.IsHost() && Online.IsOnline){
			switch(choice){
				case 1:
					Unpause();
					break;
				case 2:
					SceneTransitioner.RpcReturnToLobby();
					break;
				case 3:
					GetTree().Paused = false;
					Engine.TimeScale = 1;
					Game.Paused = false;
					Quit();
					break;
			}
		}else{
			switch(choice){
				case 1:
					Unpause();
					break;
				case 2:
					GetTree().Paused = false;
					Engine.TimeScale = 1;
					Game.Paused = false;
					Quit();
					break;
			}
		}
	}

    public override void MenuBack(){
        Unpause();
    }

    private void Unpause(){
		if(Game.MouseMode != Game.MouseModeEnum.Cursor) Input.MouseMode = Input.MouseModeEnum.Hidden;
		Game.Paused = false;
		if(!Online.IsOnline){
			GetTree().Paused = false;
			MusicPlayer.PauseMusic(false);
		}
		Engine.TimeScale = timeScale;
		QueueFree();
	}

	private void Quit(){
		if(Online.IsOnline){
			Online.Disconnect("Quit");
		}else if(Tour.IsTour){
			MenuScene.MenuToLoad = "TourMenu";
		}else{
			if(Game.TotalPlayers == 1) MenuScene.MenuToLoad = "SoloMenu";
			else if(Game.CurrentMode == Mode.GameMode.Miscellaneous) MenuScene.MenuToLoad = "VsMenu";
			else MenuScene.MenuToLoad = "ModeMenu";
		}
		SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
	}
}