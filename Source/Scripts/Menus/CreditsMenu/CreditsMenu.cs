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
		totalSelections = 4;
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
			case 2: MenuScene.LoadMenu("Credits/AssetCreditsMenu"); break;
			case 3: MenuScene.LoadMenu("Credits/AddonCreditsMenu"); break;
			case 4: MenuScene.LoadMenu("Credits/GodotCreditsMenu"); break;
		}
	}

	public override void MenuBack(){
		MenuScene.LoadMenu("MainMenu");
		SFX.Play("Back");
	}
}