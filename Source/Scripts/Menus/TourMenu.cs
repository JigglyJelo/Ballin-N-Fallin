using Godot;

public partial class TourMenu : VerticalMenu, ILeftRightSelections{
	private Label pointsText,itemsText,stompText,eventsText,teamsText,advancedText,startText;
	
	public override void _Ready(){
		base._Ready();
		totalSelections = 5;
		pointsText = GetNode<Label>("Selections/PointsText");
		itemsText = GetNode<Label>("Selections/ItemsText");
		eventsText = GetNode<Label>("EventsText");
		stompText = GetNode<Label>("Selections/StompText");
		teamsText = GetNode<Label>("TeamsText");
		advancedText = GetNode<Label>("AdvancedText");
		startText = GetNode<Label>("Selections/StartText200");
		UpdateSelectionVisual();
		UpdateTexts();
	}

	public override void _Process(double delta){
		InputChecks(delta,(int)Game.PlayerDatas[0].InputDevice);
	}

	public override void MenuBack(){
		SFX.Play("Back");
		MenuScene.LoadMenu("VsMenu");
	}

	public void MenuRight(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		switch(Selection){
			case 1:
				if(Tour.CurrentTour.PointsToWin < 1000) Tour.CurrentTour.PointsToWin += 10;
				else Tour.CurrentTour.PointsToWin = 10;
				break;
			case 2: 
				Tour.CurrentTour.ItemsEnabled = !Tour.CurrentTour.ItemsEnabled;
				break;
			case 3:
				switch(Game.StompSetting){
					case Game.StompSettingEnum.On: Game.StompSetting = Game.StompSettingEnum.TeamAttack; break;
					case Game.StompSettingEnum.TeamAttack: Game.StompSetting = Game.StompSettingEnum.Off; break;
					case Game.StompSettingEnum.Off: Game.StompSetting = Game.StompSettingEnum.On; break;
				}
				break;
		}
		joystickTimer = 0;
		UpdateTexts();
	}

	public void MenuLeft(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		switch(Selection){
			case 1:
				if(Tour.CurrentTour.PointsToWin > 10) Tour.CurrentTour.PointsToWin -= 10;
				else Tour.CurrentTour.PointsToWin = 1000;
				break;
			case 2: 
				Tour.CurrentTour.ItemsEnabled = !Tour.CurrentTour.ItemsEnabled;
				break;
			case 3:
				switch(Game.StompSetting){
					case Game.StompSettingEnum.On: Game.StompSetting = Game.StompSettingEnum.Off; break;
					case Game.StompSettingEnum.TeamAttack: Game.StompSetting = Game.StompSettingEnum.On; break;
					case Game.StompSettingEnum.Off: Game.StompSetting = Game.StompSettingEnum.TeamAttack; break;
				}
				break;
		}
		joystickTimer = 0;
		UpdateTexts();
	}

	protected override void MenuChoose(int choice){
		switch(choice){
			case 4:
				SFX.Play("Confirm");
				ModeMenu.ModeToggleMenu = true;
				MenuScene.LoadMenu("ModeMenu");
				break;
			case 5:
				SFX.Play("Confirm");
				Tour.PrepareTour();
				MenuScene.MenuBackgroundFadeout();
				SceneTransitioner.SwitchToScene(Game.SceneType.Game);
				break;
		}
	}

	private void UpdateTexts(){
		pointsText.Text = "Points to Win: " + Tour.CurrentTour.PointsToWin;
		itemsText.Text = Tour.CurrentTour.ItemsEnabled ? "Items: On" : "Items: Off";
		eventsText.Text = Tour.CurrentTour.EventsEnabled ? "Events: On" : "Events: Off";
		stompText.Text = "Stomping: " + Game.StompEnumToString(Game.StompSetting);
	}
}