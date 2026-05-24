using System.Collections.Generic;
using System.Text;
using Godot;

public partial class SoundtrackMenu : VerticalMenu{
	private string currentDirectory = "C:/";
	private List<string> openedFolders = new List<string>();
	private Node selectionsNode;
	private Polygon2D backButtonPolygon,selectButtonPolygon;
	public override void _Ready(){
		Selection = 1;
		openedFolders.Add(currentDirectory);
		//directoryInput = GetNode<TextInput>("DirectoryEntry");
		selectionsNode = GetNode<Node>("Selections");
		backButtonPolygon = GetNode<Polygon2D>("BackDirectoryButton");
		selectButtonPolygon = GetNode<Polygon2D>("SelectButton");
		UpdateDirectories();
	}

	public override void _Process(double delta){
		if(Menu.IsMouseOverPolygon(backButtonPolygon)){
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			if(Input.IsActionJustReleased("Charge N Launch Mouse")){
				GoBackDirectory();
				return;
			}
		}else if(Menu.IsMouseOverPolygon(selectButtonPolygon)){
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			if(Input.IsActionJustReleased("Charge N Launch Mouse")){
				Game.CustomSoundtrack = currentDirectory;
				LoadMenuMusic();
				return;
			}
		}
		if(Input.IsActionJustReleased("Charge N Launch Mouse")){
			foreach(Label directoryLabel in selectionsNode.GetChildren()){
				if(Menu.IsMouseOverLabel(directoryLabel)){
					DirectoryEntered(directoryLabel.Text);
					return;
				}
			}
		}
		InputChecks(delta);
	}

	private void UpdateDirectories(){
		Selections = null;
		foreach(Node child in selectionsNode.GetChildren()){
			child.QueueFree();
		}
		float position = -900;
		Selection = 1;
		totalSelections = 0;
		foreach(string directory in DirAccess.GetDirectoriesAt(currentDirectory)){
			Label directoryLabel = GD.Load<PackedScene>("res://Scenes/Object Scenes/Menus/LevelLabel.tscn").Instantiate<Label>();
			directoryLabel.Text = directory;
			directoryLabel.Name = directory+"050";
			directoryLabel.Position = new Vector2(-1920,position);
			position += 100;
			directoryLabel.Scale = new Vector2(0.5f,0.5f);
			selectionsNode.AddChild(directoryLabel);
			totalSelections++;
		}
	}

	private void DirectoryEntered(string newDirectory){
		openedFolders.Add(newDirectory);
		currentDirectory = FoldersToDirectory();
		UpdateDirectories();
	}

	private void GoBackDirectory(){
		openedFolders.RemoveAt(openedFolders.Count - 1);
		currentDirectory = FoldersToDirectory();
		UpdateDirectories();
	}

	private string FoldersToDirectory(){
		StringBuilder stringBuilder = new StringBuilder();
		foreach(string directory in openedFolders){
			stringBuilder.Append(directory);
			stringBuilder.Append("/");
		}
		return stringBuilder.ToString();
	}

	private void LoadMenuMusic(){
		MenuScene.MenuNode.Music.Playing = false;
		AudioStream stream = MusicPlayer.GetCustomSong("Menu");
		if(stream != null) MenuScene.MenuNode.Music.Stream = stream;
		else MenuScene.MenuNode.Music.Stream = GD.Load<AudioStream>("res://Assets/Music/Menu.ogg");
		MenuScene.MenuNode.Music.Playing = true;
	}

    protected override void MenuChoose(int choice){
        DirectoryEntered((selectionsNode.GetChildren()[choice-1] as Label).Text);
    }

	public override void MenuBack(){
		Game.Save.SetValue("Sound","Custom Soundtrack",Game.CustomSoundtrack);
		Game.Save.Save(Game.SETTINGS_PATH);
		MenuScene.LoadMenu("Settings/SettingsMenu");
		SFX.Play("Back");
		QueueFree();
	}
}