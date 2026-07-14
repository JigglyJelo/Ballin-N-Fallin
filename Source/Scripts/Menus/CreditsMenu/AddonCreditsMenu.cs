using Godot;

public partial class AddonCreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private Label licenseLabel, subheaderLabel;
	private Node2D selectionsNode;
	
	private float startY;
	// Adjust this value if the bottom of the text gets cut off or has too much empty space. 
	// It represents the height of the visible area for the license text.
	private float visibleTextHeight = 400f; 

	public override void _Ready(){
		base._Ready();
		totalSelections = 5;
		subheaderLabel = GetNode<Label>("LinkSubheader");
		licenseLabel = GetNode<Label>("LicenseLabel");
		selectionsNode = GetNode<Node2D>("Selections");
		
		// Capture the initial Y position as our top boundary
		startY = licenseLabel.Position.Y;
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
		ShowLicense(true);
		SFX.Play("Confirm");
	}

	public override void MenuBack(){
		if(displayingSubCredits){
			ShowLicense(false);
		}else{
			MenuScene.LoadMenu("CreditsMenu/CreditsMenu");
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
		subheaderLabel.Visible = !show;
		selectionsNode.Visible = !show;
		
		// Reset the scroll position to the top every time a new license is opened
		if(show){
			licenseLabel.Position = new Vector2(licenseLabel.Position.X, startY);
		}
		
		licenseLabel.Text = show ? getLicenseString(selectionsNode.GetChild<Label>(Selection - 1).Text.Split('|')[0].Trim()) : "";

		string getLicenseString(string addonName){
			string[] possibleNames = { "LICENSE", "LICENSE.md", "LICENSE.txt", "license.md", "License.txt" };
			foreach(string fileName in possibleNames){
				string path = $"res://addons/{addonName}/{fileName}";
				if(FileAccess.FileExists(path)){
					using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
					return file.GetAsText();
				}
			}
			return "No license found";
		}
	}
}