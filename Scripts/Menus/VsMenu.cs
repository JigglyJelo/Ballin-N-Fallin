using Godot;

public partial class VsMenu : Menu2D{
	private Label tourText,freeplayText,howToPlayText;
	private Polygon2D tourButton,freeplayButton,howToPlayButton;
	public override void _Ready(){
		base._Ready();
		tourText = GetNode<Label>("Selections/TourButton/TourText");
		tourButton = GetNode<Polygon2D>("Selections/TourButton");
		freeplayText = GetNode<Label>("Selections/FreeplayButton/FreeplayText");
		freeplayButton = GetNode<Polygon2D>("Selections/FreeplayButton");
		howToPlayText = GetNode<Label>("Selections/HowToPlayButton/HowToPlayText");
		howToPlayButton = GetNode<Polygon2D>("Selections/HowToPlayButton");
		Selection = Tour.IsTour ? 1 : 2;
		UpdateSelectionVisual();
	}


	public override void _Process(double delta){
		InputChecks(delta,(int)Game.PlayerDatas[0].InputDevice);
	}

	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		switch(choice){
			case 1:
				Tour.IsTour = true;
				GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "TourMenu.tscn").Instantiate());
				QueueFree();
				break;
			case 2:
				Tour.IsTour = false;
				GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "ModeMenu.tscn").Instantiate());
				QueueFree();
				break;
			default:
				Tour.IsTour = false;
				Game.CurrentMode = Mode.GameMode.Miscellaneous;
				Game.SetLevel(Game.CurrentMode,"HowToPlayLevel.tscn");
				//Game.CurrentMode = Mode.GameMode.Domination;
				//Game.SetLevel(Game.CurrentMode,"Test.tscn");
				MenuScene.MenuBackgroundFadeout();
				SceneTransitioner.SwitchToScene(Game.SceneType.Game);
				break;
		}
	}

	public override void MenuBack(){
		SFX.Play("Back");
		GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "PlayerMenu.tscn").Instantiate());
		QueueFree();
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
				tourText.SelfModulate = SELECTED_COLOR;
				tourButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				freeplayText.SelfModulate = Colors.White;
				freeplayButton.Color = BUTTON_COLOR;
				howToPlayText.SelfModulate = Colors.White;
				howToPlayButton.Color = BUTTON_COLOR;
				break;
			case 2:
				//Selected
				freeplayText.SelfModulate = SELECTED_COLOR;
				freeplayButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				tourText.SelfModulate = Colors.White;
				tourButton.Color = BUTTON_COLOR;
				howToPlayText.SelfModulate = Colors.White;
				howToPlayButton.Color = BUTTON_COLOR;
				break;
			default:
				//Selected
				howToPlayText.SelfModulate = SELECTED_COLOR;
				howToPlayButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				tourText.SelfModulate = Colors.White;
				tourButton.Color = BUTTON_COLOR;
				freeplayText.SelfModulate = Colors.White;
				freeplayButton.Color = BUTTON_COLOR;
				break;
		}
	}
}