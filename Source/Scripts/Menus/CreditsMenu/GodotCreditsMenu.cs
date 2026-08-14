using Godot;
using System.Text;

public partial class GodotCreditsMenu : Menu{
	[Export]
	public float VisibleAreaHeight = 600f;

	private Label licenseLabel;
	private float topLimit;

	public override void _Ready(){
		licenseLabel = GetNode<Label>("LicenseLabel");
		licenseLabel.Text = GetLicensesString();
		topLimit = licenseLabel.Position.Y;
	}

	public override void _Process(double delta){
		float bottomLimit = topLimit - Mathf.Max(0, licenseLabel.Size.Y - VisibleAreaHeight);

		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("B" + i)){
				MenuBack();
				return;
			}else{
				float y = Input.GetVector("Aim Left" + i, "Aim Right" + i, "Aim Up" + i, "Aim Down" + i).Y;
				if(y > 0.5f && licenseLabel.Position.Y > bottomLimit){
					licenseLabel.Position -= new Vector2(0,(float)delta * 400);
					return;
				}else if(y < -0.5f && licenseLabel.Position.Y < topLimit){
					licenseLabel.Position += new Vector2(0,(float)delta * 400);
					return;
				}
			}
		}
		if(Input.IsActionJustReleased("ScrollWheelUp") && licenseLabel.Position.Y < topLimit){
			licenseLabel.Position += new Vector2(0,(float)delta * 4000);
			return;
		}else if(Input.IsActionJustReleased("ScrollWheelDown") && licenseLabel.Position.Y > bottomLimit){
			licenseLabel.Position -= new Vector2(0,(float)delta * 4000);
			return;
		}
	}

	protected override void InputChecks(double delta, int inputId){}
	protected override void InputChecks(double delta){}
	protected override void UpdateSelectionVisual(){}
	protected override void MenuChoose(int choice){}

	public override void MenuBack(){
		MenuScene.LoadMenu("Credits/CreditsMenu");
	}

	public static string GetLicensesString(){
		StringBuilder fullText = new StringBuilder();
		fullText.Append("Made in Godot " + Engine.GetVersionInfo()["major"] + "." + Engine.GetVersionInfo()["minor"] + "." + Engine.GetVersionInfo()["patch"]);
		fullText.Append("========================================\n");
		fullText.Append("          GODOT ENGINE LICENSE          \n");
		fullText.Append("========================================\n\n");
		fullText.Append(Engine.GetLicenseText() + "\n\n\n");
		
		fullText.Append("========================================\n");
		fullText.Append("        THIRD-PARTY LICENSES            \n");
		fullText.Append("========================================\n\n");
		
		Godot.Collections.Dictionary licenses = Engine.GetLicenseInfo();
		foreach (Variant key in licenses.Keys){
			fullText.Append("--- " + (string)key + " ---\n");
			fullText.Append((string)licenses[key] + "\n\n");
		}
		
		fullText.Append("========================================\n");
		fullText.Append("        COPYRIGHT ATTRIBUTIONS          \n");
		fullText.Append("========================================\n\n");
		
		Godot.Collections.Array<Godot.Collections.Dictionary> copyrights = Engine.GetCopyrightInfo();
		foreach(Godot.Collections.Dictionary item in copyrights){
			Godot.Collections.Dictionary component = item;
			fullText.Append("Component: " + (string)component["name"] + "\n");
			
			Godot.Collections.Array parts = (Godot.Collections.Array)component["parts"];
			foreach(Variant partItem in parts){
				Godot.Collections.Dictionary part = (Godot.Collections.Dictionary)partItem;
				fullText.Append("  License: " + (string)part["license"] + "\n");
				fullText.Append("  Copyright Holders: \n");
				
				Godot.Collections.Array copyrightHolders = (Godot.Collections.Array)part["copyright"];
				foreach(Variant copyrightHolder in copyrightHolders){
					fullText.Append("    - " + (string)copyrightHolder + "\n");
				}
			}
			fullText.Append("----------------------------------------\n");
		}
		
		return fullText.ToString();
	}
}