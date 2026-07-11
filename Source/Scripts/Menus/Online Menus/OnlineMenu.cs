using Godot;
using System.Text.RegularExpressions;

public partial class OnlineMenu : Menu2D{
	private LineEdit usernameInput;
	private Label hostText,joinText,directText;
	private Polygon2D hostButton,joinButton,directButton;
	public override void _Ready(){
		base._Ready();
		usernameInput = GetNode<LineEdit>("UsernameEntry");
		hostText = GetNode<Label>("Selections/HostButton/HostText");
		hostButton = GetNode<Polygon2D>("Selections/HostButton");
		joinText = GetNode<Label>("Selections/JoinButton/JoinText");
		joinButton = GetNode<Polygon2D>("Selections/JoinButton");
		directText = GetNode<Label>("Selections/DirectButton/DirectText");
		directButton = GetNode<Polygon2D>("Selections/DirectButton");
		usernameInput.Text = Online.Username.Equals("Player") ? "" : Online.Username;
		Online.InputId = PlayerData.PlayerInputDevice.None;
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Charge N Launch" + i)) Online.InputId = (PlayerData.PlayerInputDevice)i;
		}
		if(Game.UsingMouse() && Online.InputId == PlayerData.PlayerInputDevice.None && Input.IsActionJustReleased("Charge N Launch Mouse")){
			if(Game.MouseMode == Game.MouseModeEnum.Off) Game.MouseMode = Game.MouseModeEnum.Cursor;
			Online.InputId = PlayerData.PlayerInputDevice.Mouse;
		}
		InputChecks(delta);
	}

	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		Online.Username = ParseUsername();
		switch(choice){
			case 1:
				Online.Network = Online.NetworkType.Noray;
				HostLobby();
				break;
			case 2:
				MenuScene.LoadMenu("Online/LobbyBrowserMenu");
				break;
			default:
				Online.Network = Online.NetworkType.Direct;
				MenuScene.LoadMenu("Online/DirectConnectMenu");
				break;
		}
	}

	private void HostLobby(){
		OnlineLobby lobby = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlineLobby.tscn").Instantiate<OnlineLobby>();
		lobby.IsHost = true;
		MenuScene.CurrentMenuNode = lobby; //Manually deal with menu stuff here
		GetParent().AddChild(lobby);
		QueueFree();
	}

	public override void MenuBack(){
		SFX.Play("Back");
		MenuScene.LoadMenu("MainMenu");
	}

	protected override void MenuRight(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		if(Selection == 1 || Selection == 3) Selection++;
		else if(Selection == 2) Selection = 1;
		else Selection = 3;
		UpdateSelectionVisual();
	}

	protected override void MenuLeft(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		if(Selection == 2 || Selection == 3) Selection--;
		else if(Selection == 1) Selection = 2;
		else Selection = 3;
		UpdateSelectionVisual();
	}

	protected override void MenuUp(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		if(Selection > 2) Selection -= 2;
		UpdateSelectionVisual();
	}

	protected override void MenuDown(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		if(Selection <= 2) Selection += 2;
		UpdateSelectionVisual();
	}

	protected override void UpdateSelectionVisual(){
		switch(Selection){
			case 1:
				//Selected
				hostText.SelfModulate = SELECTED_COLOR;
				hostButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				joinText.SelfModulate = Colors.White;
				joinButton.Color = BUTTON_COLOR;
				directText.SelfModulate = Colors.White;
				directButton.Color = BUTTON_COLOR;
				break;
			case 2:
				//Selected
				joinText.SelfModulate = SELECTED_COLOR;
				joinButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				hostText.SelfModulate = Colors.White;
				hostButton.Color = BUTTON_COLOR;
				directText.SelfModulate = Colors.White;
				directButton.Color = BUTTON_COLOR;
				break;
			default:
				//Selected
				directText.SelfModulate = SELECTED_COLOR;
				directButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				hostText.SelfModulate = Colors.White;
				hostButton.Color = BUTTON_COLOR;
				joinText.SelfModulate = Colors.White;
				joinButton.Color = BUTTON_COLOR;
				break;
		}
	}

	private string ParseUsername(){
		if(!usernameInput.Text.Equals("")){
			string text = Regex.Replace(usernameInput.Text, "[^a-zA-Z0-9 ]", "");
			try{
				return text.Substring(0,Online.USERNAME_LENGTH);
			}catch{
				return text;
			}
		}
		return "Player";
	}
}