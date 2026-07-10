using Godot;
using System;
using System.Collections.Generic;

public partial class LevelMenu : VerticalMenu{
    private static List<string> optionNames;
	public static List<string> FoldersOpened;
	private string lastMenu;
	private Node selectionsNode;
	private short yPos = -600;
	private int inputId = (int)Game.PlayerDatas[0].InputDevice; //Online.IsOnline ? Online.PlayerInfos[0].InputId : Game.InputIds[0];

	public override void _Ready(){
		base._Ready();
		Tour.ResetPlayerScores();
		if(FoldersOpened == null) FoldersOpened = FoldersOpened = new List<string> {""};
		optionNames = new List<string>();
		if(Game.TotalPlayers > 1 || Online.IsOnline) lastMenu = "ModeMenu";
		else lastMenu = "SoloMenu";
		selectionsNode = GetNode<Node>("Selections");
		GetNode<Label>("Label").Text = Mode.EnumToString(Game.CurrentMode) + " Levels";
		int index = 0;
		foreach(string folder in DirAccess.GetDirectoriesAt(Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + string.Join("",FoldersOpened))){
			Label folderLabel = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn").Instantiate<Label>();
			
			optionNames.Add(folder + "/");
			folderLabel.Text = folder;
			folderLabel.Name = "Folder" + index;
			folderLabel.Position = new Vector2(-1920,yPos);
			folderLabel.Scale = Vector2.One;
			yPos += 200;
			selectionsNode.AddChild(folderLabel);
			index++;
		}
		foreach(string file in DirAccess.GetFilesAt(Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + string.Join("",FoldersOpened))){
			Label levelLabel = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn").Instantiate<Label>();
			string newFile = file;
			if(file.Contains(".remap")) newFile = file.Replace(".remap","");
			optionNames.Add(newFile);
			levelLabel.Text = newFile.Replace(".tscn","");
			levelLabel.Name = "Level" + index;
			levelLabel.Position = new Vector2(-1920,yPos);
			levelLabel.Scale = Vector2.One;
			yPos += 200;
			selectionsNode.AddChild(levelLabel);
			index++;
		}
		Selection = 1;
		totalSelections = optionNames.Count;
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(!Game.UsingMouse() && inputId < (int)PlayerData.PlayerInputDevice.Mouse){
			InputChecks(delta,inputId);
			if(Input.IsActionJustReleased("Y" + inputId)){
				Selection = new Random().Next(1,optionNames.Count + 1);
				UpdateSelectionVisual();
        	}	
		}else InputChecks(delta);
	}
	//Either starts the level that's selected or opens the folder that's selected
	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		foreach(Node node in selectionsNode.GetChildren()){
			Label label;
			if(node is Label) label = node as Label;
			else break;
			//Start Level
			if(label.Name.ToString().Equals("Level" + (choice - 1))){
				if(Game.TotalPlayers == 1){
					if(Game.CurrentMode == Mode.GameMode.Race) RaceHUD.LevelName = string.Join("",FoldersOpened) + optionNames[choice - 1];
					else if(Game.CurrentMode == Mode.GameMode.Survival) SurvivalHUD.LevelName = string.Join("",FoldersOpened) + optionNames[choice - 1];
				}
				Game.SetLevel(Game.CurrentMode,optionNames[choice - 1],string.Join("",FoldersOpened));
				
				if(!Online.IsOnline){
					MenuScene.MenuBackgroundFadeout();
					SceneTransitioner.SwitchToScene(Game.SceneType.Game);
				}else{
					if(OnlineLobby.Lobby != null) OnlineLobby.Lobby.StartGame();
				}
				
			//Open Folder
			}else if(label.Name.ToString().Equals("Folder" + (choice - 1)) && (!GolfCup.IsCup || !(optionNames[choice - 1].Contains("Cup")))){
				FolderNavigation(true);
			//Start Golf Cup
			}else if(label.Name.ToString().Equals("Folder" + (choice - 1)) && GolfCup.IsCup && optionNames[choice - 1].Contains("Cup")){
				GolfCup.PrepareCup(FoldersOpened);
				MenuScene.MenuBackgroundFadeout();
				SceneTransitioner.SwitchToScene(Game.SceneType.Game);
			}
		}
		
	}
	//Either returns back to the last Menu if not in a folder else exits the folder
	public override void MenuBack(){
		SFX.Play("Back");
		if(FoldersOpened.Count <= 1){
			if(lastMenu == "ModeMenu" && Online.IsOnline){
				MenuScene.MenuNode.AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "ModeMenu" + ".tscn").Instantiate<Node>());
            	QueueFree();
			}else{
				MenuScene.LoadMenu(lastMenu);
			}
		}else{
			FolderNavigation(false);
		}
        
    }
	//Colors current selection green
	
	protected override void UpdateSelectionVisual(){
		base.UpdateSelectionVisual();
		foreach(Node node in selectionsNode.GetChildren()){
			Label label = node as Label;
			if(label.Name.ToString().Contains("Level")){
				if(label.Name.Equals("Level" + (Selection - 1))) label.SelfModulate = SELECTED_COLOR;
				else label.SelfModulate = Colors.White;
			}else if(label.Name.ToString().Contains("Folder")){
				if(label.Name.Equals("Folder" + (Selection - 1))) label.SelfModulate = new Color(0,0.5f,1);
				else label.SelfModulate = new Color(0.5f,0.75f,1);
			}
		}
	}
	
	///<summary>Opens or closes a folder.</summary><param name="opening">true to open, false to close.</param>
	private void FolderNavigation(bool opening){
		if(opening) FoldersOpened.Add(optionNames[Selection - 1]);
		else FoldersOpened.RemoveAt(FoldersOpened.Count - 1);
		MenuScene.LoadMenu("LevelMenu");
	}
}