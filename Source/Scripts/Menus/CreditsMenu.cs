using Godot;

public partial class CreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private Label headerLabel, subheaderLabel, codeLabel, musicLabel,sfxLabel;
	private const float VISIBLE_AREA_HEIGHT = 600f;
	
	public override void _Ready(){
		base._Ready();
		Selection = 1;
		headerLabel = GetNode<Label>("CreditsHeader");
		subheaderLabel = GetNode<Label>("LinkSubheader");
		codeLabel = GetNode<Label>("CodeCredits");
		musicLabel = GetNode<Label>("MusicCredits");
		sfxLabel = GetNode<Label>("SFXCredits");
		totalSelections = 5;
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(displayingSubCredits){
			float topLimit = -838f;
			float bottomLimit = topLimit - Mathf.Max(0, (musicLabel.Size.Y * musicLabel.Scale.Y) - VISIBLE_AREA_HEIGHT);

			//Only check for back button
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("B" + i)){
					MenuBack();
					return;
				}else{
					float y = Input.GetVector("Aim Left" + i, "Aim Right" + i, "Aim Up" + i, "Aim Down" + i).Y;
					if(y > 0.5f && musicLabel.Position.Y > bottomLimit){
						musicLabel.Position -= new Vector2(0,(float)delta * 400);
						return;
					}else if(y < -0.5f && musicLabel.Position.Y < topLimit){
						musicLabel.Position += new Vector2(0,(float)delta * 400);
						return;
					}
				}
			}
			if(Input.IsActionJustReleased("ScrollWheelUp") && musicLabel.Position.Y < topLimit){
				musicLabel.Position += new Vector2(0,(float)delta * 4000);
				return;
			}else if(Input.IsActionJustReleased("ScrollWheelDown") && musicLabel.Position.Y > bottomLimit){
				musicLabel.Position -= new Vector2(0,(float)delta * 4000);
				return;
			}
		}else{
			InputChecks(delta);
		}
	}

	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		switch(Selection){
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
		codeLabel.Visible = subCredits == 1;
		musicLabel.Visible = subCredits == 2;
		sfxLabel.Visible = subCredits == 3;
		switch(subCredits){
			case 0: headerLabel.Text = "Ballin N Fallin by JigglyJello"; break;
			case 2:
				headerLabel.Text = "Music Used";
				musicLabel.Text = GetMusicCredits();
				musicLabel.Position = new Vector2(-1920, -838);
				break;
			case 3: headerLabel.Text = "SFX Used"; break;
		}
	}

	private static string GetMusicCredits(){
		using FileAccess file = FileAccess.Open("res://Assets/Music/Music Credits.txt", FileAccess.ModeFlags.Read);
		if(file != null) return file.GetAsText();
		return null; 
	}
}