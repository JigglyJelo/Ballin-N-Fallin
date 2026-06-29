using Godot;
using System.Linq;

public partial class PlayerSettingsMenu : Node2D{
	public int Id;
	public int InputId;
	public static int JoinedPlayers = 0;
	public static int ReadyPlayers = 0;
	public bool isReady = false;

	public Sprite2D playerBallSprite, playerShadingSprite;
	private Label profileLabel;

	private ColorMenu colorMenu;
	private MiniProfileMenu profileMenu;

	private bool inProfileMenu = false;

	public override void _Ready(){
		if(Online.IsOnline){
			InputId = (int)Online.InputId;
			if(!Game.UsingMouse() && Online.InputId == PlayerData.PlayerInputDevice.Mouse){
				Game.MouseMode = Game.MouseModeEnum.Cursor;
			}
		}else{
			if(!Game.UsingMouse()) InputId = Online.IsOnline ? (int)Online.InputId : (int)Game.PlayerDatas[Id-1].InputDevice;
			else InputId = (int)PlayerData.PlayerInputDevice.Mouse;
		}

		playerBallSprite = GetNode<Sprite2D>("Player/BallSprite");
		playerShadingSprite = GetNode<Sprite2D>("Player/ShadingSprite");
		profileLabel = GetNode<Label>("ProfileLabel");

		colorMenu = GetNode<ColorMenu>("ColorMenu");
		profileMenu = GetNode<MiniProfileMenu>("MiniProfileMenu");

		colorMenu.Init(this); 
		profileMenu.OnProfileSelected += HandleProfileSelected;
		profileMenu.OnCanceled += HandleProfileCanceled;

		JoinedPlayers++;

		if(!Online.IsOnline){
			Game.PlayerDatas[Id-1].PlayerColor = ColorMenu.DefaultColorOrder.First(color => !Game.PlayerDatas.Any(player => player.PlayerColor == color));
		}else{
			Id = Game.PlayerDatas.FindIndex(player => player.UUID == Game.GameNode.Multiplayer.GetUniqueId())+1;
		}
		profileLabel.Text = GetDisplayName();
		if(Game.UsingMouse()){
			profileLabel.Visible = false;
		}

		SetPlayerColor(Game.PlayerDatas[Id-1].PlayerColor);

		if(Game.TotalPlayers > 4) Scale = new Vector2(0.7f,0.7f);
		if(Online.IsOnline) Position = new Vector2(0,300);
		else SetPosition();

		SwitchToColorMenu(); 
	}

	public override void _Process(double delta){
		if(isReady) return;

		if(!Game.UsingMouse() && InputId != (int)PlayerData.PlayerInputDevice.Mouse){
			if(profileMenu.IsKeypadOpen) return;

			if((Game.PlayerDatas.Count >= Id || Online.IsOnline) && Input.IsActionJustReleased("Y" + InputId)){
				if(inProfileMenu) HandleProfileCanceled(); 
				else SwitchToProfileMenu();
			}
		}
	}

	private void SwitchToColorMenu(){
		inProfileMenu = false;
		colorMenu.Visible = true;
		colorMenu.SetProcess(true);
		profileMenu.Visible = false;
		profileMenu.SetProcess(false);
		SFX.Play("Move");
	}

	private void SwitchToProfileMenu(){
		inProfileMenu = true;
		colorMenu.Visible = false;
		colorMenu.SetProcess(false);
		profileMenu.Visible = true;
		profileMenu.SetProcess(true);
		profileMenu.Open(InputId, Game.PlayerDatas[Id-1].ControlProfileName);
		SFX.Play("Move");
	}

	private void HandleProfileSelected(string newProfile){
		Game.PlayerDatas[Id-1].ControlProfileName = newProfile;
		profileLabel.Text = GetDisplayName();
		SwitchToColorMenu();
	}

	private string GetDisplayName(){
		return Game.PlayerDatas[Id-1].ControlProfileName == ControlProfileManager.DEFAULT_PROFILE ? "Player " + Id : Game.PlayerDatas[Id-1].ControlProfileName;
	}

	private void HandleProfileCanceled(){
		SwitchToColorMenu();
	}

	public void SetPlayerColor(Color color){
		playerBallSprite.SelfModulate = color;
		playerShadingSprite.SelfModulate = color;
	}

	public void SetPosition(){
		switch(Game.TotalPlayers){
			case 1: Scale = Vector2.One; Position = new Vector2(0,300); break;
			case 2: Scale = Vector2.One; Position = (Id == 1) ? new Vector2(-600,300) : new Vector2(600,300); break;
			case 3:
				Scale = Vector2.One;
				switch(Id){
					case 1: Position = new Vector2(-1000, 300); break;
					case 2: Position = new Vector2(0, 300); break;
					case 3: Position = new Vector2(1000, 300); break;
				}
				break;
			case 4:
				Scale = new Vector2(0.9f,0.9f);
				switch(Id){
					case 1: Position = new Vector2(-1350,300); break;
					case 2: Position = new Vector2(-450,300); break;
					case 3: Position = new Vector2(450,300); break;
					case 4: Position = new Vector2(1350,300); break;
				}
				break;
			case 5:
				Scale = new Vector2(0.7f,0.7f);
				switch(Id){
					case 1: Position = new Vector2(-1000,-190); break;
					case 2: Position = new Vector2(0,-190); break;
					case 3: Position = new Vector2(1000,-190); break;
					case 4: Position = new Vector2(-600,743); break;
					case 5: Position = new Vector2(600,743); break;
				}
				break;
			case 6:
				Scale = new Vector2(0.7f,0.7f);
				switch(Id){
					case 1: Position = new Vector2(-1000,-190); break;
					case 2: Position = new Vector2(0,-190); break;
					case 3: Position = new Vector2(1000,-190); break;
					case 4: Position = new Vector2(-1000,743); break;
					case 5: Position = new Vector2(0,743); break;
					case 6: Position = new Vector2(1000,743); break;
				}
				break;
			case 7:
				Scale = new Vector2(0.7f,0.7f);
				switch(Id){
					case 1: Position = new Vector2(-1350, -190); break;
					case 2: Position = new Vector2(-450, -190); break;
					case 3: Position = new Vector2(450, -190); break;
					case 4: Position = new Vector2(1350, -190); break;
					case 5: Position = new Vector2(-1000, 743); break;
					case 6: Position = new Vector2(0, 743); break;
					case 7: Position = new Vector2(1000, 743); break;
				}
				break;
			case 8:
				Scale = new Vector2(0.7f,0.7f);
				switch(Id){
					case 1: Position = new Vector2(-1350, -190); break;
					case 2: Position = new Vector2(-450, -190); break;
					case 3: Position = new Vector2(450, -190); break;
					case 4: Position = new Vector2(1350, -190); break;
					case 5: Position = new Vector2(-1350, 743); break;
					case 6: Position = new Vector2(-450, 743); break;
					case 7: Position = new Vector2(450, 743); break;
					case 8: Position = new Vector2(1350, 743); break;
				}
				break;
		}
		profileLabel.Text = GetDisplayName();
	}
}