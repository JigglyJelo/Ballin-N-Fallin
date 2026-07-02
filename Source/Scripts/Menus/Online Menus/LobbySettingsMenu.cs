using Godot;
using System;

public partial class LobbySettingsMenu : VerticalMenu, ILeftRightSelections{
	private Label lobbbyText,modeText,pointsText,itemsText,stompText,bufferText,startText,colorText;
	public bool InColorMenu = false;
	private float colorChangeTimer = 0;
	public override void _Ready(){
		base._Ready();
		totalSelections = 7;
		lobbbyText = GetNode<Label>("LobbyText");
		modeText = GetNode<Label>("Selections/ModeText066");
		pointsText = GetNode<Label>("Selections/ScoreText066");
		itemsText = GetNode<Label>("Selections/ItemsText066");
		stompText = GetNode<Label>("Selections/StompText066");
		bufferText = GetNode<Label>("Selections/BufferText066");
		startText = GetNode<Label>("Selections/StartText");
		colorText = GetNode<Label>("ColorText");
		if(Game.UsingMouse()){
			colorText.Text = "Click to change color";
			colorText.GetNode<Sprite2D>("ButtonPrompt").Visible = false;
		}
		UpdateSelectionVisual();
		UpdateTexts();
		if(!IsMultiplayerAuthority()){
			startText.Visible = false;
			Selection = totalSelections;
		}
		if(!Online.IsHost()){
			GetNode<Node2D>("ArrowSelections").Visible = false;
			if(Game.UsingMouse()) GetNode<Node2D>("MenuBackButton").Modulate = Colors.White;
		} 
	}

	public override void _Process(double delta){
		if(!InColorMenu && Online.IsConnected()){
			if(colorChangeTimer < 1) colorChangeTimer += (float)delta;
			if(Online.IsHost()){
				if(OnlineLobby.Lobby.LobbyShown){
					try{
						int inputId = (int)Game.PlayerDatas[0].InputDevice;
						InputChecks(delta,inputId);
						if(!InColorMenu){
							if(Game.UsingMouse() || inputId == (int)PlayerData.PlayerInputDevice.Mouse){
								if(Input.IsActionJustReleased("Charge N Launch Mouse") && hoveredOverColorText()) createColorMenu();
							}else{
								if(Input.IsActionJustReleased("Slam" + inputId)) createColorMenu();
							}
						}
					}catch{}
					if(Input.IsKeyPressed(Key.B)) EnterSpectatorMode();
				}
			}else if(OnlineLobby.Lobby.LobbyShown){
				if(Game.UsingMouse()){
					if(Input.IsActionJustReleased("Charge N Launch Mouse") && hoveredOverColorText() && colorChangeTimer >= 1) createColorMenu();
				}else{
					for(int i = 0; i < Game.MAX_PLAYERS; i++){
						if(Input.IsActionJustReleased("B" + i)) MenuBack();
						if(Input.IsActionJustReleased("Slam" + i) && colorChangeTimer >= 1) createColorMenu();
					}
				}
			}
		}
		void createColorMenu(){
			InColorMenu = true;
			AddChild(GD.Load<PackedScene>("res://Source/Scenes/Menus/PlayerSettingsMenu/PlayerSettingsMenu.tscn").Instantiate());
		}
		bool hoveredOverColorText(){
			Vector2 mousePosition = colorText.GetLocalMousePosition();
			return mousePosition.X > 294 && mousePosition.X < 2016 && mousePosition.Y > 44 && mousePosition.Y < 200;
		}
	}

	protected override void UpdateSelectionVisual(){
		if(IsMultiplayerAuthority()){
			modeText.SelfModulate = (Selection == 1) ? SELECTED_COLOR : Colors.White;
			itemsText.SelfModulate = (Selection == 3) ? SELECTED_COLOR : Colors.White;
			if(Tour.IsTour){
				pointsText.SelfModulate = (Selection == 2) ? SELECTED_COLOR : Colors.White;
			}else{
				pointsText.SelfModulate = (Selection == 2) ? new Color(1,0,0) : new Color(0.5f,0.5f,0.5f);
			}
			bufferText.SelfModulate = (Selection == 4) ? SELECTED_COLOR : Colors.White;
			startText.SelfModulate = (Selection == 5) ? SELECTED_COLOR : Colors.White;
			SelectionGrowVisual();
		}else{
			modeText.SelfModulate = Colors.White;
			pointsText.SelfModulate = Colors.White;
			itemsText.SelfModulate = Colors.White;
			bufferText.SelfModulate = Colors.White;
			startText.SelfModulate = Game.ZEROES;
		}
	}

	private void SelectionGrowVisual(){
        //Get Selections list
        if(Selections == null) Selections = GetNode("Selections").GetChildren();
        else if(Selections.Count < 1) return;
        //Set colors and size for all options
        for(int i = 0; i < Selections.Count; i++){
            Node selectionOption = Selections[i];
            if(selectionOption is Label textOption){
                const float SIZE = 1 + (1/3f);
                if(Selection == i+1){ //Current Selection becomes green and grows
                    float bigFontScale;
                    try{
					    bigFontScale = int.Parse(textOption.Name.ToString().Substring(textOption.Name.ToString().Length-3))*SIZE/100f;
				    }catch{
					    bigFontScale = defaultFontSize*SIZE;
				    }
                    textOption.SelfModulate = SELECTED_COLOR;
                    textOption.ZIndex = 1;
                    Tween scaleTween = CreateTween();
                    float scaleSize = bigFontScale;
                    scaleTween.TweenProperty(textOption,"scale",new Vector2(scaleSize,scaleSize),0.15f);
                }else{ //Previous selection becomes white and shrinks
                    float normalFontScale;
                    try{
					    normalFontScale = int.Parse(textOption.Name.ToString().Substring(textOption.Name.ToString().Length-3))/100f;
				    }catch{
					    normalFontScale = defaultFontSize;
				    }
                    textOption.SelfModulate = Colors.White;
                    textOption.ZIndex = 0;
                    Tween scaleTween = CreateTween();
                    scaleTween.TweenProperty(textOption,"scale",new Vector2(normalFontScale,normalFontScale),0.15f);
                }
            }
        }
    }

	public override void MenuBack(){
		Online.Disconnect("Left Lobby");
	}

	public void MenuRight(){
		switch(Selection){
			case 1:
				Tour.IsTour = !Tour.IsTour;
				UpdateSelectionVisual();
				break;
			case 2:
				if(Tour.CurrentTour.PointsToWin < 1000) Tour.CurrentTour.PointsToWin += 10;
				else Tour.CurrentTour.PointsToWin = 10;
				break;
			case 3: 
				Tour.CurrentTour.ItemsEnabled = !Tour.CurrentTour.ItemsEnabled;
				break;
			case 4:
				switch(Game.StompSetting){
					case Game.StompSettingEnum.On: Game.StompSetting = Game.StompSettingEnum.TeamAttack; break;
					case Game.StompSettingEnum.TeamAttack: Game.StompSetting = Game.StompSettingEnum.Off; break;
					case Game.StompSettingEnum.Off: Game.StompSetting = Game.StompSettingEnum.On; break;
				}
				break;
			case 6:
				if(MathF.Abs(Online.Buffer - 0) < 0.01f) Online.Buffer = 0.5f;
				else if(MathF.Abs(Online.Buffer - 0.5f) < 0.01f) Online.Buffer = 1;
				else Online.Buffer = 0;
				GD.Print(Online.Buffer);
				break;
				
		}
		joystickTimer = 0;
		OnlineLobby.Lobby.Rpc(nameof(OnlineLobby.Lobby.UpdateLobbySettings),Tour.IsTour,Tour.CurrentTour.PointsToWin,Tour.CurrentTour.ItemsEnabled,(byte)Game.StompSetting,Online.Buffer);
	}

	public void MenuLeft(){
		switch(Selection){
			case 1:
				Tour.IsTour = !Tour.IsTour;
				UpdateSelectionVisual();
				break;
			case 2:
				if(Tour.CurrentTour.PointsToWin > 10) Tour.CurrentTour.PointsToWin -= 10;
				else Tour.CurrentTour.PointsToWin = 1000;
				break;
			case 3: 
				Tour.CurrentTour.ItemsEnabled = !Tour.CurrentTour.ItemsEnabled;
				break;
			case 4:
				switch(Game.StompSetting){
					case Game.StompSettingEnum.On: Game.StompSetting = Game.StompSettingEnum.Off; break;
					case Game.StompSettingEnum.TeamAttack: Game.StompSetting = Game.StompSettingEnum.On; break;
					case Game.StompSettingEnum.Off: Game.StompSetting = Game.StompSettingEnum.TeamAttack; break;
				}
				break;
			case 6:
				if(MathF.Abs(Online.Buffer - 1) < 0.01f) Online.Buffer = 0.5f;
				else if(MathF.Abs(Online.Buffer - 0.5f) < 0.01f) Online.Buffer = 0;
				else Online.Buffer = 1;
				GD.Print(Online.Buffer);
				break;
		}
		joystickTimer = 0;
		OnlineLobby.Lobby.Rpc(nameof(OnlineLobby.Lobby.UpdateLobbySettings),Tour.IsTour,Tour.CurrentTour.PointsToWin,Tour.CurrentTour.ItemsEnabled,(byte)Game.StompSetting,Online.Buffer);
	}

	protected override void MenuChoose(int choice){
		if(Online.IsHost()){
			switch(choice){
				case 5:
					OnlineLobby.ShowLobby(false);
					ModeMenu.ModeToggleMenu = true;
					GetParent().GetParent().AddChild(GD.Load<PackedScene>("res://Source/Scenes/Menus/ModeMenu.tscn").Instantiate());		
					break;
				case 7:
					if(Tour.IsTour){
						Tour.PrepareTour();
						OnlineLobby.Lobby.StartGame();
						QueueFree();
					}else{
						ModeMenu.ModeToggleMenu = false;
						OnlineLobby.ShowLobby(false);
						GetParent().GetParent().AddChild(GD.Load<PackedScene>("res://Source/Scenes/Menus/ModeMenu.tscn").Instantiate());		
					}
					break;
			}
		}
	}

	public void UpdateTexts(){
		modeText.Text = "Mode: " + (Tour.IsTour ? "Tour" : "Freeplay");
		pointsText.Text = "Points to Win: " + Tour.CurrentTour.PointsToWin;
		itemsText.Text = "Items: " + (Tour.CurrentTour.ItemsEnabled ? "On" : "Off");
		stompText.Text = "Stomping: " + Game.StompEnumToString(Game.StompSetting);
		bufferText.Text = "Buffer: " + BufferToString(Online.Buffer);
	}

	private string BufferToString(float buffer){
		switch(buffer){
			case 1f: return "Full";
			case 0.5f: return "Half";
			case 0f: return "None";
			default: return buffer.ToString();
		}
	}

	private void EnterSpectatorMode(){
		GD.Print("Spectatin");
		PlayerData yourPlayerInfo;
		foreach(PlayerData playerInfo in Game.PlayerDatas){
			if(playerInfo.UUID == Multiplayer.GetUniqueId()){
				yourPlayerInfo = playerInfo;
			}
		}
		Rpc(nameof(EnterSpectatorModeRpc));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void EnterSpectatorModeRpc(){
		int specatorUUID = Online.GetRpcSender();
		PlayerData spectatorInfo = null;
		foreach(PlayerData playerInfo in Game.PlayerDatas){
			if(specatorUUID == playerInfo.UUID){
				spectatorInfo = playerInfo;
				break;
			}
		}
		if(Game.PlayerDatas.Contains(spectatorInfo)){
			Game.SpectatorDatas.Add(spectatorInfo);
			int index = Game.PlayerDatas.IndexOf(spectatorInfo);
			Game.PlayerDatas.Remove(spectatorInfo);
			//if(index != -1) Online.PlayerColors.RemoveAt(index);
		}
		GetParent<OnlineLobby>().UpdatePlayerTexts();
	}
}