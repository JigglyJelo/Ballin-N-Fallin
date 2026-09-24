using Godot;

public partial class AssetCreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private CreditsPanel creditsPanel;
	private Label headerLabel, subheaderLabel;
	private Node2D selectionsNode;
	
	private float startY;
	private float visibleTextHeight = 400f; 

	public override void _Ready(){
		base._Ready();
		Selection = 1;
		totalSelections = 3;
		headerLabel = GetNode<Label>("CreditsHeader");
		creditsPanel = GetNode<CreditsPanel>("Panel");
		subheaderLabel = GetNode<Label>("LinkSubheader");
		selectionsNode = GetNode<Node2D>("Selections");
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(displayingSubCredits){
			//Only check for back button
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("B" + i)){
					MenuBack();
					return;
				}
			}
		}else{
			InputChecks(delta);
		}
	}

	protected override void MenuChoose(int choice){
		ShowLicense(true, choice);
		SFX.Play("Confirm");
	}

	public override void MenuBack(){
		if(displayingSubCredits){
			ShowLicense(false, 0);
		}else{
			MenuScene.LoadMenu("Credits/CreditsMenu");
		}
		SFX.Play("Back");
	}

	private void ShowLicense(bool show, int selection){
		displayingSubCredits = show;
		if(Selections == null) Selections = GetNode("Selections").GetChildren();
		foreach(Node node in Selections){
			if(node is Label label){
				label.Visible = !show;
			}
		}
		headerLabel.Visible = !show;
		subheaderLabel.Visible = !show;
		selectionsNode.Visible = !show;
		creditsPanel.Visible = show;
		creditsPanel.LicenseText = show ? getLicenseString(selection) : "";

		string getLicenseString(int selection){
			switch(selection){
				case 1:
					if(FileAccess.FileExists("res://Assets/Sprites/Image Credits.txt")){
						using FileAccess file = FileAccess.Open("res://Assets/Sprites/Image Credits.txt", FileAccess.ModeFlags.Read);
						return file.GetAsText();
					}
					break;
				case 2:
					string returnText = "";
					if(FileAccess.FileExists("res://Assets/Music/Music Credits.txt")){
						using FileAccess musicTextFile = FileAccess.Open("res://Assets/Music/Music Credits.txt", FileAccess.ModeFlags.Read);
						returnText += musicTextFile.GetAsText();
						returnText += "\n";
						if(FileAccess.FileExists("res://Assets/SFX/SFX Credits.txt")){
							using FileAccess sfxTextFile = FileAccess.Open("res://Assets/SFX/SFX Credits.txt", FileAccess.ModeFlags.Read);
							returnText += sfxTextFile.GetAsText();
						}
					}
					if(returnText != ""){
						return returnText;
					}
					break;
				case 3:
					if(FileAccess.FileExists("res://Assets/Font/Open Font License.txt")){
						using FileAccess file = FileAccess.Open("res://Assets/Font/Open Font License.txt", FileAccess.ModeFlags.Read);
						return file.GetAsText();
					}
					break;
			}
			return "Error: License file not found";
		}
	}
}