using Godot;

public partial class AddonCreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private CreditsPanel creditsPanel;
	private Label headerLabel, subheaderLabel;
	private Control selectionsNode;
	
	

	public override void _Ready(){
		base._Ready();
		Selection = 1;
		totalSelections = 5;
		creditsPanel = GetNode<CreditsPanel>("Panel");
		headerLabel = GetNode<Label>("CreditsHeader");
		subheaderLabel = GetNode<Label>("LinkSubheader");
		selectionsNode = GetNode<Control>("Selections");
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(displayingSubCredits){
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
		ShowLicense(true);
		SFX.Play("Confirm");
	}

	public override void MenuBack(){
		if(displayingSubCredits){
			ShowLicense(false);
		}else{
			MenuScene.LoadMenu("Credits/CreditsMenu");
		}
		SFX.Play("Back");
	}

	private void ShowLicense(bool show){
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
		
		creditsPanel.LicenseText = show ? getLicenseString(selectionsNode.GetChild<Label>(Selection - 1).Text.Split('|')[0].Trim()) : "";

		string getLicenseString(string addonName){
			string[] possibleNames = { "LICENSE", "LICENSE.md", "LICENSE.txt", "license.md", "License.txt" };
			foreach(string fileName in possibleNames){
				string path = $"res://addons/{addonName}/{fileName}";
				if(FileAccess.FileExists(path)){
					using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
					return file.GetAsText();
				}
			}
			return "Error: License not found.";
		}
	}

	protected override void UpdateSelectionVisual(){
		if(Selections == null) Selections = GetNode("Selections").GetChildren();

		foreach(Node selection in Selections){
			if(selection is Label label){
				label.PivotOffset = label.Size / 2f;
			}
		}
		
		base.UpdateSelectionVisual();
	}
}