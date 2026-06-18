using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerMenu : Node2D{
	private Label topText,backButtonText;
	private Polygon2D backPolygon;
	private PackedScene playerSettingsMenuScene = GD.Load<PackedScene>("res://Source/Scenes/Menus/PlayerSettingsMenu/PlayerSettingsMenu.tscn");
	public static List<Color> selectedColors; //Keeps track of what colors are currently selected so no repeats
	public static List<PlayerSettingsMenu> SettingMenus = new List<PlayerSettingsMenu>();

    public override void _Ready(){
		//Game.InputIds = new List<byte>();
		Game.PlayerDatas = new List<PlayerData>();
		SettingMenus = new List<PlayerSettingsMenu>();
		selectedColors = new List<Color>();
		PlayerSettingsMenu.JoinedPlayers = 0;
		PlayerSettingsMenu.ReadyPlayers = 0;
		topText = GetNode<Label>("Label");
		backButtonText = GetNode<Label>("MenuBackButton/BackText");
		backPolygon = GetNode<Polygon2D>("MenuBackButton/BackArrow");
		if (!Game.UsingMouse()){
			GetNode<Node2D>("MenuBackButton").Visible = false;
			Cursor.UsingCursor = false;
			Input.MouseMode = Input.MouseModeEnum.Hidden;
		}else{
			Cursor.UsingCursor = true;
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		Game.TotalPlayers = 0;
    }
	
    public override void _Process(double delta){
		if(Game.TotalPlayers > 0 && !Game.UsingMouse()){
			for(int i = 0; i < Game.TotalPlayers; i++){
				if(Input.IsActionJustReleased("Start" + (int)Game.PlayerDatas[i].InputDevice)) MenuStart();
				//Get to Vs Menu in 1 Player for testing
				if(Input.IsActionJustReleased("Item" + (int)Game.PlayerDatas[i].InputDevice)){
					MenuScene.LoadMenu("VsMenu");
				}
			}
		}else if(Game.TotalPlayers == 1 && Game.UsingMouse() && Input.IsActionJustReleased("Start Keyboard")){
			MenuStart();
		}

		float xSize = Mathf.Abs(topText.Size.X/2f);
		float ySize = Mathf.Abs(topText.Size.Y/2f);
		float xDist = Mathf.Abs((topText.GlobalPosition.X + xSize) - GetGlobalMousePosition().X);
		float yDist = Mathf.Abs((topText.GlobalPosition.Y + ySize) - GetGlobalMousePosition().Y);
		if(xDist < xSize && yDist < ySize && PlayerSettingsMenu.ReadyPlayers == 1){
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			topText.SelfModulate = new Color(0,1,0);
			if(Game.UsingMouse() && Input.IsActionJustReleased("Charge N Launch Mouse")){
				MenuStart();
			}
		}else{
			topText.SelfModulate = Colors.White;
		}		
				
		if(Menu.IsMouseOverLabel(backButtonText) || Menu.IsMouseOverPolygon(backPolygon)){
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			backButtonText.SelfModulate = new Color(0,1,0);
			backPolygon.Color = backButtonText.SelfModulate;
			if(Game.UsingMouse() && Input.IsActionJustReleased("Charge N Launch Mouse")){
				MenuBack();
			}
		}else{
			backButtonText.SelfModulate = Colors.White;
			backPolygon.Color = Colors.White;
		}

		if(Game.UsingMouse()){
			if(PlayerSettingsMenu.JoinedPlayers == 0){
				topText.Text = "Click to Join";
			}else if(PlayerSettingsMenu.ReadyPlayers != PlayerSettingsMenu.JoinedPlayers){
				topText.Text = "Choose a Color!";
			}else{
				topText.Text = "Click here to Start!";
			}
		}else{
			if(PlayerSettingsMenu.ReadyPlayers != PlayerSettingsMenu.JoinedPlayers || PlayerSettingsMenu.JoinedPlayers == 0 && PlayerSettingsMenu.JoinedPlayers != Game.MAX_PLAYERS) topText.Text = "Press A to Join";
			else if(PlayerSettingsMenu.JoinedPlayers == Game.MAX_PLAYERS) topText.Text = "Choose Colors!";
			else if(PlayerSettingsMenu.JoinedPlayers == 1) topText.Text = "Ready: Press Start to play Solo!";
			else topText.Text = "Ready: Press Start!";
		}
		//Join Mouse
		if(Game.TotalPlayers == 0 && Input.IsActionJustPressed("Charge N Launch Mouse")){
			SFX.Play("PlayerEnter");
			Game.TotalPlayers++;
			PlayerSettingsMenu newMenu = playerSettingsMenuScene.Instantiate<PlayerSettingsMenu>();
			//Game.InputIds.Add(1);
			Game.PlayerDatas.Add(new PlayerData("PM",PlayerData.PlayerInputDevice.Mouse,1));
			newMenu.Id = 1;
			SettingMenus.Add(newMenu);
			AddChild(newMenu);
			foreach(PlayerSettingsMenu menu in SettingMenus) menu.SetPosition();
		}
		//Join Controllers
		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Charge N Launch" + i) && !Game.PlayerDatas.Any(player => (int)player.InputDevice == i)){
				SFX.Play("PlayerEnter");
				Game.TotalPlayers++;
				//Game.InputIds.Add((byte)i);
				Game.PlayerDatas.Add(new PlayerData("P"+(i+1),(PlayerData.PlayerInputDevice)i,1));
				PlayerSettingsMenu newMenu = playerSettingsMenuScene.Instantiate<PlayerSettingsMenu>();
				//newMenu.Id = (byte)Game.InputIds.Count;
				newMenu.Id = Game.PlayerDatas.Count;
				SettingMenus.Add(newMenu);
				AddChild(newMenu);
				foreach(PlayerSettingsMenu menu in SettingMenus) menu.SetPosition();
			}
		}

		if(PlayerSettingsMenu.JoinedPlayers == 0){
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("B" + i)){
					MenuBack();
					break; //Needed cause Godot is hjjjhgffn;lgk and makes it so pressing Esc counts as B for every player even though there is not a single input event mapped to Esc anywhere in the entire project
				}
			}
		}
	}

	public void MenuStart(){
		SFX.Play("Confirm");
		if(PlayerSettingsMenu.ReadyPlayers == PlayerSettingsMenu.JoinedPlayers){
			foreach(Node node in GetChildren()) QueueFree();
			if(Game.TotalPlayers == 1) MenuScene.LoadMenu("SoloMenu");
			else MenuScene.LoadMenu("VsMenu");
		}
    }

	private void MenuBack(){
		SFX.Play("Back");
		Game.MouseMode = Game.MouseModeEnum.Off;
		MenuScene.LoadMenu("MainMenu");
	}
}