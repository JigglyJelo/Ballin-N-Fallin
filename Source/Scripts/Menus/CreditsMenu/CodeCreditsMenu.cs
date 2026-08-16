using Godot;

public partial class CodeCreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private Label licenseLabel, subheaderLabel;
	private Node2D selectionsNode;
	
	private float startY;
	// Adjust this value if the bottom of the text gets cut off or has too much empty space. 
	// It represents the height of the visible area for the license text.
	private float visibleTextHeight = 400f; 

	public override void _Ready(){
		base._Ready();
		Selection = 1;
		totalSelections = 3;
		subheaderLabel = GetNode<Label>("LinkSubheader");
		licenseLabel = GetNode<Label>("LicenseLabel");
		selectionsNode = GetNode<Node2D>("Selections");
		
		// Capture the initial Y position as our top boundary
		startY = licenseLabel.Position.Y;
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(displayingSubCredits){
			// Dynamically calculate the bottom limit based on the label's actual height and scale
			float bottomLimit = startY - Mathf.Max(0, (licenseLabel.Size.Y * licenseLabel.Scale.Y) - visibleTextHeight);

			//Only check for back button
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("B" + i)){
					MenuBack();
					return;
				}else{
					float y = Input.GetVector("Aim Left" + i, "Aim Right" + i, "Aim Up" + i, "Aim Down" + i).Y;
					if(y > 0.5f && licenseLabel.Position.Y > bottomLimit){
						float newY = Mathf.Clamp(licenseLabel.Position.Y - (float)delta * 400, bottomLimit, startY);
						licenseLabel.Position = new Vector2(licenseLabel.Position.X, newY);
						return;
					}else if(y < -0.5f && licenseLabel.Position.Y < startY){
						float newY = Mathf.Clamp(licenseLabel.Position.Y + (float)delta * 400, bottomLimit, startY);
						licenseLabel.Position = new Vector2(licenseLabel.Position.X, newY);
						return;
					}
				}
			}
			if(Input.IsActionJustReleased("ScrollWheelUp") && licenseLabel.Position.Y < startY){
				float newY = Mathf.Clamp(licenseLabel.Position.Y + (float)delta * 4000, bottomLimit, startY);
				licenseLabel.Position = new Vector2(licenseLabel.Position.X, newY);
				return;
			}else if(Input.IsActionJustReleased("ScrollWheelDown") && licenseLabel.Position.Y > bottomLimit){
				float newY = Mathf.Clamp(licenseLabel.Position.Y - (float)delta * 4000, bottomLimit, startY);
				licenseLabel.Position = new Vector2(licenseLabel.Position.X, newY);
				return;
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
		subheaderLabel.Visible = !show;
		selectionsNode.Visible = !show;
		
		// Reset the scroll position to the top every time a new license is opened
		if(show){
			licenseLabel.Position = new Vector2(licenseLabel.Position.X, startY);
		}
		
		licenseLabel.Text = show ? getLicenseString(selection) : "";

		string getLicenseString(int selection){
			switch(selection){
				case 1:
					if(FileAccess.FileExists("res://LICENSE")){
						using FileAccess file = FileAccess.Open("res://LICENSE", FileAccess.ModeFlags.Read);
						return file.GetAsText();
					}
					break;
				case 2:
					if(FileAccess.FileExists("res://LICENSE-ADDENDUM.md")){
						using FileAccess file = FileAccess.Open("res://LICENSE-ADDENDUM.md", FileAccess.ModeFlags.Read);
						return file.GetAsText();
					}
					break;
				case 3:
					string returnText = "";
					if(FileAccess.FileExists("res://Levels/README.txt")){
						using FileAccess file = FileAccess.Open("res://Levels/README.txt", FileAccess.ModeFlags.Read);
						returnText += file.GetAsText();
					}
					if(FileAccess.FileExists("res://Levels/LICENSE.md")){
						using FileAccess file = FileAccess.Open("res://Levels/LICENSE.md", FileAccess.ModeFlags.Read);
						returnText += file.GetAsText();
					}
					if(returnText != ""){
						return returnText;
					}
					break;
			}
			return "No license found";
		}
	}
}