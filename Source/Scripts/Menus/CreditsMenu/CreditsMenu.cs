using Godot;

public partial class CreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private Label headerLabel, subheaderLabel, creditsLabel;
	private const float VISIBLE_AREA_HEIGHT = 600f;
	
	public override void _Ready(){
		base._Ready();
		Selection = 1;
		headerLabel = GetNode<Label>("CreditsHeader");
		subheaderLabel = GetNode<Label>("LinkSubheader");
		creditsLabel = GetNode<Label>("CreditsLabel");
		totalSelections = 5;
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(displayingSubCredits){
			float topLimit = -838f;
			float bottomLimit = topLimit - Mathf.Max(0, (creditsLabel.Size.Y * creditsLabel.Scale.Y) - VISIBLE_AREA_HEIGHT);

			//Only check for back button
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("B" + i)){
					MenuBack();
					return;
				}else{
					float y = Input.GetVector("Aim Left" + i, "Aim Right" + i, "Aim Up" + i, "Aim Down" + i).Y;
					if(y > 0.5f && creditsLabel.Position.Y > bottomLimit){
						creditsLabel.Position -= new Vector2(0,(float)delta * 400);
						return;
					}else if(y < -0.5f && creditsLabel.Position.Y < topLimit){
						creditsLabel.Position += new Vector2(0,(float)delta * 400);
						return;
					}
				}
			}
			if(Input.IsActionJustReleased("ScrollWheelUp") && creditsLabel.Position.Y < topLimit){
				creditsLabel.Position += new Vector2(0,(float)delta * 4000);
				return;
			}else if(Input.IsActionJustReleased("ScrollWheelDown") && creditsLabel.Position.Y > bottomLimit){
				creditsLabel.Position -= new Vector2(0,(float)delta * 4000);
				return;
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
		subheaderLabel.Visible = !displayingSubCredits;
		creditsLabel.Visible = subCredits == 2 || subCredits == 3;
		switch(subCredits){
			case 0: headerLabel.Text = "Ballin N Fallin by JigglyJello"; break;
			case 2:
				headerLabel.Text = "Music Used";
				creditsLabel.Text = GetMusicCredits();
				creditsLabel.Position = new Vector2(-1920, -838);
				break;
			case 3:
				headerLabel.Text = "SFX Used";
				creditsLabel.Text = GetSFXCredits();
				creditsLabel.Position = new Vector2(-1920, -838);
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