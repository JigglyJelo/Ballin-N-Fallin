using Godot;

public partial class CreditsMenu : VerticalMenu{
	private bool displayingSubCredits = false;
	private Label headerLabel, subheaderLabel, codeLabel, musicLabel,sfxLabel,miscLabel;
	private const string MUSIC_CREDITS_TEXT = @"
		Menu: The Gallant Seventh - US Marine Band
		Score Screen: Semper Fidelis - US Marine Band
		Victory: Stars & Stripes Forever - US Marine Band
		Race: William Tell Overture Finale - US Marine Band
		Golf: Strauss Waltz Medley - USAF Band
		King of the Hill: Can-Can (by Offenbach) - European Archive
		Deathmatch: Danse Baccanale from Samson et Dalila - USAF Band
		Soccer: Washington Post March - US Marine Band
		Crown the King: Egmont Overture Finale Kevin MacLeod (incompetech.com) 
		     Licensed under Creative Commons: By Attribution 4.0 License
		     http://creativecommons.org/licenses/by/4.0/
		Survival: Flight of the Bumblebees - US Army Band
		Volleyball: The Blue Danube - US Marine Band
		Hot Potato: Hall of the Mountain King - Kevin MacLeod (incompetech.com) 
		     Licensed under Creative Commons: By Attribution 4.0 License
		     http://creativecommons.org/licenses/by/4.0/
		Ballin to the Bank: Russian Dance - Lud and Schlatts Musical Emporium 
			 Licensed under Creative Commons: By Attribution 3.0 License
			 https://creativecommons.org/licenses/by/3.0/
		Domination: Radetzky March - US Marine Band
		Target Test: Carmen - Prelude, Act I - European Archive
		Payload: Galop from Genevieve de Brabant - United States Marine Band
		Bomb Ball: Go Falcons + Falcons Fight - US Air Force Band of the Rockies
	";
	
	public override void _Ready(){
		base._Ready();
		headerLabel = GetNode<Label>("CreditsHeader");
		subheaderLabel = GetNode<Label>("LinkSubheader");
		codeLabel = GetNode<Label>("CodeCredits");
		musicLabel = GetNode<Label>("MusicCredits");
		sfxLabel = GetNode<Label>("SFXCredits");
		miscLabel = GetNode<Label>("MiscCredits");
		totalSelections = 4;
	}

    public override void _Process(double delta){
        if(displayingSubCredits){
			//Only check for back button
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("B" + i)){
					MenuBack();
					return;
				}else{
					float y = Input.GetVector("Aim Left" + i, "Aim Right" + i, "Aim Up" + i, "Aim Down" + i).Y;
					if(y > 0.5f && musicLabel.Position.Y > -1228){
						musicLabel.Position -= new Vector2(0,(float)delta * 400);
						return;
					}else if(y < -0.5f && musicLabel.Position.Y < -838){
						musicLabel.Position += new Vector2(0,(float)delta * 400);
						return;
					}
				}
			}
			if(Input.IsActionJustReleased("ScrollWheelUp") && musicLabel.Position.Y < -838){
				musicLabel.Position += new Vector2(0,(float)delta * 4000);
				return;
			}else if(Input.IsActionJustReleased("ScrollWheelDown") && musicLabel.Position.Y > -1228){
				musicLabel.Position -= new Vector2(0,(float)delta * 4000);
				return;
			}
		}else{
			InputChecks(delta);
		}
    }

    protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		if(Selection != 4){
			ShowSubCredits(choice);
		}else{
			MenuScene.LoadMenu("AddonCreditsMenu");
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
		miscLabel.Visible = subCredits == 4;
		switch(subCredits){
			case 0: headerLabel.Text = "Ballin N Fallin by JigglyJello"; break;
			case 2:
				headerLabel.Text = "Music Used";
				musicLabel.Text = MUSIC_CREDITS_TEXT;
				musicLabel.Position = new Vector2(-1920, -838);
				break;
			case 3: headerLabel.Text = "SFX Used"; break;
			case 4: headerLabel.Text = "Misc Credits"; break;
		}
	}
}