using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class OnlineMenu : VerticalMenu{
	private Label joinLabel,hostLabel;
	private TextInput usernameInput, ipInput, portInput;

	public override void _Ready(){
		base._Ready();
		Online.IsOnline = false;
		Game.PlayerDatas = new List<PlayerData>();
		Game.SpectatorDatas = new List<PlayerData>();
		hostLabel = GetNode<Label>("Selections/Host200");
		joinLabel = GetNode<Label>("Selections/Join200");
		ipInput = GetNode<TextInput>("IpEntry");
		portInput = GetNode<TextInput>("PortEntry");
		usernameInput = GetNode<TextInput>("UsernameEntry");
		ipInput.InputString = Online.Address;
		portInput.InputString = Online.Port.ToString();
		usernameInput.InputString = Online.Username.Equals("Player") ? "" : Online.Username;
		totalSelections = 2;
		defaultFontSize = 2;
		UpdateSelectionVisual();
		if(Game.IsDedicatedServer) MenuChoose(1);
		Online.Network = Online.NetworkType.Direct;
	}

	public override void _Process(double delta){
		//InputChecks(delta);
		for(byte i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Charge N Launch" + i)) Online.InputId = (PlayerData.PlayerInputDevice)i;
		}
		if(Input.IsActionJustReleased("Charge N Launch Mouse")){
			Online.InputId = PlayerData.PlayerInputDevice.Mouse;
		}
		InputChecks(delta);
	}

	private void HostLobby(){
		//if(!ipInput.InputString.Equals("")) Online.Address = ipInput.InputString;
		Online.Username = ParseUsername();
		OnlineLobby lobby = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlineLobby.tscn").Instantiate<OnlineLobby>();
		lobby.IsHost = true;
		GetParent().AddChild(lobby);
		QueueFree();
	}

	private void JoinLobby(){
		if(!ipInput.InputString.Equals("")) Online.Address = ipInput.InputString;
		Online.Username = ParseUsername();
		ParsePort();
		OnlineLobby lobby = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlineLobby.tscn").Instantiate<OnlineLobby>();
		lobby.IsHost = false;
		GetParent().AddChild(lobby);
		QueueFree();
	}

	private string ParseUsername(){
		if(!usernameInput.InputString.Equals("")){
			string text = Regex.Replace(usernameInput.InputString, "[^a-zA-Z0-9 ]", "");
			try{
				return text.Substring(0,Online.USERNAME_LENGTH);
			}catch{
				return text;
			}
		}
		return "Player";
	}

	private bool ParsePort(){
		if(!portInput.InputString.Equals("")){
			try{
				Online.Port = ushort.Parse(portInput.InputString);
				return true;
			}catch{
				return false;
			}
		}
		Online.Port = Online.DEFAULT_PORT;
		return true;
	}

    protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		if(choice == 1) HostLobby();
		else JoinLobby();
	}

    public override void MenuBack(){
		SFX.Play("Back");
		Online.IsOnline = false;
		GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "MainMenu.tscn").Instantiate());
        QueueFree();
    }
}