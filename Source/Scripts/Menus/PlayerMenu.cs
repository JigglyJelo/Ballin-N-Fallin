using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerMenu : Node2D{
	private Label topText,backButtonText;
	private Polygon2D backPolygon;
	public static List<Color> selectedColors; //Keeps track of what colors are currently selected so no repeats
	public static List<ColorMenu> ColorMenus = new List<ColorMenu>();

    public override void _Ready(){
		//Game.InputIds = new List<byte>();
		Game.PlayerDatas = new List<PlayerData>();
		ColorMenus = new List<ColorMenu>();
		selectedColors = new List<Color>();
		ColorMenu.JoinedPlayers = 0;
		ColorMenu.ReadyPlayers = 0;
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
				if(Input.IsActionJustReleased("Slam" + (int)Game.PlayerDatas[i].InputDevice)){
					GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "VsMenu.tscn").Instantiate());
					QueueFree();
				}
			}
		}else if(Game.TotalPlayers == 1 && Game.UsingMouse() && Input.IsActionJustReleased("Start Keyboard")){
			MenuStart();
		}

		float xSize = Mathf.Abs(topText.Size.X/2f);
		float ySize = Mathf.Abs(topText.Size.Y/2f);
		float xDist = Mathf.Abs((topText.GlobalPosition.X + xSize) - GetGlobalMousePosition().X);
		float yDist = Mathf.Abs((topText.GlobalPosition.Y + ySize) - GetGlobalMousePosition().Y);
		if(xDist < xSize && yDist < ySize && ColorMenu.ReadyPlayers == 1){
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
			if(ColorMenu.JoinedPlayers == 0){
				topText.Text = "Click to Join";
			}else if(ColorMenu.ReadyPlayers != ColorMenu.JoinedPlayers){
				topText.Text = "Choose a Color!";
			}else{
				topText.Text = "Click here to Start!";
			}
		}else{
			if(ColorMenu.ReadyPlayers != ColorMenu.JoinedPlayers || ColorMenu.JoinedPlayers == 0 && ColorMenu.JoinedPlayers != Game.MAX_PLAYERS) topText.Text = "Press A to Join";
			else if(ColorMenu.JoinedPlayers == Game.MAX_PLAYERS) topText.Text = "Choose Colors!";
			else if(ColorMenu.JoinedPlayers == 1) topText.Text = "Ready: Press Start to play Solo!";
			else topText.Text = "Ready: Press Start!";
		}
		//Join Mouse
		if(Game.TotalPlayers == 0 && Input.IsActionJustPressed("Charge N Launch Mouse")){
			SFX.Play("PlayerEnter");
			Game.TotalPlayers++;
			ColorMenu newMenu = GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Players/ColorMenu.tscn").Instantiate<ColorMenu>();
			//Game.InputIds.Add(1);
			Game.PlayerDatas.Add(new PlayerData("PM",PlayerData.PlayerInputDevice.Mouse,1));
			newMenu.Id = 1;
			ColorMenus.Add(newMenu);
			AddChild(newMenu);
			foreach(ColorMenu menu in ColorMenus) menu.SetPosition();
		}
		//Join Controllers
		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Charge N Launch" + i) && !Game.PlayerDatas.Any(player => (int)player.InputDevice == i)){
				SFX.Play("PlayerEnter");
				Game.TotalPlayers++;
				//Game.InputIds.Add((byte)i);
				Game.PlayerDatas.Add(new PlayerData("P"+(i+1),(PlayerData.PlayerInputDevice)i,1));
				ColorMenu newMenu = GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Players/ColorMenu.tscn").Instantiate<ColorMenu>();
				//newMenu.Id = (byte)Game.InputIds.Count;
				newMenu.Id = Game.PlayerDatas.Count;
				ColorMenus.Add(newMenu);
				AddChild(newMenu);
				foreach(ColorMenu menu in ColorMenus) menu.SetPosition();
			}
		}

		if(ColorMenu.JoinedPlayers == 0){
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
		if(ColorMenu.ReadyPlayers == ColorMenu.JoinedPlayers){
			foreach(Node node in GetChildren()) QueueFree();
			if(Game.TotalPlayers == 1) GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "SoloMenu.tscn").Instantiate());
			else GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "VsMenu.tscn").Instantiate());
			QueueFree();
		}
    }

	private void MenuBack(){
		SFX.Play("Back");
		Game.MouseMode = Game.MouseModeEnum.Off;
		GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "MainMenu.tscn").Instantiate());
		QueueFree();
	}
}