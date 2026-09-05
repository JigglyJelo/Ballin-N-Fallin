using Godot;

public partial class CreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private CreditsPanel creditsPanel;
	private Label headerLabel, subheaderLabel;
	
	public override void _Ready(){
		base._Ready();
		Selection = 1;
		creditsPanel = GetNode<CreditsPanel>("Panel");
		headerLabel = GetNode<Label>("CreditsHeader");
		subheaderLabel = GetNode<Label>("LinkSubheader");
		totalSelections = 5;
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
		SFX.Play("Confirm");
		switch(Selection){
			case 1: MenuScene.LoadMenu("Credits/CodeCreditsMenu"); break;
			case 4: MenuScene.LoadMenu("Credits/AddonCreditsMenu"); break;
			case 5: MenuScene.LoadMenu("Credits/GodotCreditsMenu"); break;
			default: ShowSubCredits(choice); break;
		}
	}

	public override void MenuBack(){
		if(displayingSubCredits){
			ShowSubCredits(0);
		}else{
			MenuScene.LoadMenu("MainMenu");
		}
		SFX.Play("Back");
	}

	private void ShowSubCredits(int subCredits){
		displayingSubCredits = subCredits != 0;
		if(Selections == null) Selections = GetNode("Selections").GetChildren();
		foreach(Node node in Selections){
			if(node is Label label){
				label.Visible = !displayingSubCredits;
			}
		}
		headerLabel.Visible = subCredits != 2 && subCredits != 3;
		subheaderLabel.Visible = !displayingSubCredits;
		creditsPanel.Visible = subCredits == 2 || subCredits == 3;
		switch(subCredits){
			case 0:
				headerLabel.Text = "Ballin N Fallin by JigglyJello";
				break;
			case 2:
				creditsPanel.LicenseText = GetMusicCredits();
				break;
			case 3:
				creditsPanel.LicenseText = GetSFXCredits();
				break;
		}
	}

	private static string GetMusicCredits(){
		using FileAccess file = FileAccess.Open("res://Assets/Music/Music Credits.txt", FileAccess.ModeFlags.Read);
		if(file != null) return file.GetAsText();
		return null; 
	}

	private static string GetSFXCredits(){
		using FileAccess file = FileAccess.Open("res://Assets/SFX/SFX Credits.txt", FileAccess.ModeFlags.Read);
		if(file != null) return file.GetAsText();
		return null; 
	}
}