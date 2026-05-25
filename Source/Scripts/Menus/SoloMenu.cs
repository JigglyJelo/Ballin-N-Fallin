using Godot;
using System;
using System.Collections.Generic;

public partial class SoloMenu : VerticalMenu{
	private Label timeTrialsText,golfText,survivalText,howToPlayText;
	public override void _Ready(){
		base._Ready();
		timeTrialsText = GetNode<Label>("Selections/TimeTrialsText");
		golfText = GetNode<Label>("Selections/GolfText");
		survivalText = GetNode<Label>("Selections/SurvivalText");
		howToPlayText = GetNode<Label>("Selections/HowToPlayText");
		Selection = 1;
		totalSelections = 4;
		UpdateSelectionVisual();
		Tour.IsTour = false;
		Game.TotalPlayers = 1;
		GolfCup.IsCup = false;
	}

	public override void _Process(double delta){
		if(!Game.UsingMouse()){
			InputChecks(delta,(int)Game.PlayerDatas[0].InputDevice);
		} 
		else InputChecks(delta);
	}

	public override void MenuBack(){
		SFX.Play("Back");
		GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "MainMenu.tscn").Instantiate());
		QueueFree();
	}

	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		LevelMenu.FoldersOpened = new List<string> {""};
		switch(choice){
			case 1: Game.CurrentMode = Mode.GameMode.Race; break;
			case 2:
				//GolfCup.IsCup = true;
				Game.CurrentMode = Mode.GameMode.Golf;
				break;
			case 3: Game.CurrentMode = Mode.GameMode.Survival; break;
			case 4: 
				Tour.IsTour = false;
				Game.CurrentMode = Mode.GameMode.Miscellaneous;
				Game.SetLevel(Game.CurrentMode,"HowToPlayLevel.tscn");
				MenuScene.MenuBackgroundFadeout();
				SceneTransitioner.SwitchToScene(Game.SceneType.Game);
				break;
		}
		GetParent().AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelMenu.tscn").Instantiate());
        QueueFree();
	}
}