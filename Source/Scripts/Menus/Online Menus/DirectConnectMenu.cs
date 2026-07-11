using Godot;
using System.Collections.Generic;

public partial class DirectConnectMenu : VerticalMenu{
	private Label joinLabel,hostLabel;
	private LineEdit ipInput, portInput;

	public override void _Ready(){
		base._Ready();
		Online.Network = Online.NetworkType.Direct;
		Online.IsOnline = false;
		Game.PlayerDatas = new List<PlayerData>();
		Game.SpectatorDatas = new List<PlayerData>();
		hostLabel = GetNode<Label>("Selections/Host200");
		joinLabel = GetNode<Label>("Selections/Join200");
		ipInput = GetNode<LineEdit>("IPEntry");
		portInput = GetNode<LineEdit>("PortEntry");
		ipInput.Text = Online.Address;
		portInput.Text = Online.Port.ToString();
		
		totalSelections = 2;
		defaultFontSize = 2;
		UpdateSelectionVisual();
		if(Game.IsDedicatedServer) MenuChoose(1);
	}

	public override void _Process(double delta){
		for(byte i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Charge N Launch" + i)) Online.InputId = (PlayerData.PlayerInputDevice)i;
		}
		if(Input.IsActionJustReleased("Charge N Launch Mouse") && Game.UsingMouse()){
			Online.InputId = PlayerData.PlayerInputDevice.Mouse;
		}
		InputChecks(delta);
	}

	private void HostLobby(){
		//if(!ipInput.InputString.Equals("")) Online.Address = ipInput.InputString;
		OnlineLobby lobby = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlineLobby.tscn").Instantiate<OnlineLobby>();
		lobby.IsHost = true;
		GetParent().AddChild(lobby);
		QueueFree();
	}

	private void JoinLobby(){
	    if(!ipInput.Text.Equals("")) Online.Address = ipInput.Text;
	    ParsePort();
	    OnlineLobby lobby = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlineLobby.tscn").Instantiate<OnlineLobby>();
	    lobby.IsHost = false;
	    GetParent().AddChild(lobby);
	    QueueFree();
	}

	private bool ParsePort(){
		if(!portInput.Text.Equals("")){
			try{
				Online.Port = ushort.Parse(portInput.Text);
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