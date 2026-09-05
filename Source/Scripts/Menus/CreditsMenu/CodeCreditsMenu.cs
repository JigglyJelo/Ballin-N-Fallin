using Godot;

public partial class CodeCreditsMenu : VerticalMenu{
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
					if(FileAccess.FileExists("res://LICENSE")){
						using FileAccess file = FileAccess.Open("res://LICENSE", FileAccess.ModeFlags.Read);
						return file.GetAsText();
					}
					break;
				case 2:
					if(FileAccess.FileExists("res://LICENSE-ADDENDUM.txt")){
						using FileAccess file = FileAccess.Open("res://LICENSE-ADDENDUM.txt", FileAccess.ModeFlags.Read);
						return file.GetAsText();
					}
					break;
				case 3:
					string returnText = "";
					if(FileAccess.FileExists("res://Levels/README.txt")){
						using FileAccess file = FileAccess.Open("res://Levels/README.txt", FileAccess.ModeFlags.Read);
						returnText += file.GetAsText();
						returnText += "\n";
					}
					if(FileAccess.FileExists("res://Levels/LICENSE.txt")){
						using FileAccess file = FileAccess.Open("res://Levels/LICENSE.txt", FileAccess.ModeFlags.Read);
						returnText += file.GetAsText();
					}
					if(returnText != ""){
						return returnText;
					}
					break;
			}
			return "Error: License file not found";
		}
	}
}