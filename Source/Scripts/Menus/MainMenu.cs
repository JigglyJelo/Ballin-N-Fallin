using Godot;

public partial class MainMenu : VerticalMenu{
	private Label playText, settingsText, creditsText, exitText,copyrightText;
	private readonly Color COPYRIGHT_COLOR = Color.Color8(192,192,192);
	private readonly Color COPYRIGHT_COLOR_HOVERED = Color.Color8(225,225,225);
	public override void _Ready(){
		base._Ready();
		Selection = 1;
		totalSelections = 4;
		defaultFontSize = 1;
		playText = GetNode<Label>("Selections/Play Text");
		settingsText = GetNode<Label>("Selections/Settings Text");
		creditsText = GetNode<Label>("Selections/Credits Text");
		exitText = GetNode<Label>("Selections/Exit Text");
		copyrightText = GetNode<Label>("Copyright");
		UpdateSelectionVisual();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "Logo.tscn").Instantiate());
	}

    public override void _Process(double delta){
        base._Process(delta);

		//Load Credits Menu
		if(IsMouseOverLabel(copyrightText)){
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			if(Input.IsActionJustReleased("Charge N Launch Mouse")){
				MenuScene.LoadMenu("Credits/CreditsMenu");
				SFX.Play("Confirm");
			}
			if(copyrightText.SelfModulate != COPYRIGHT_COLOR_HOVERED){
				copyrightText.SelfModulate = COPYRIGHT_COLOR_HOVERED;
				SFX.Play("Move");
			}
		}else if(copyrightText.SelfModulate != COPYRIGHT_COLOR){
			copyrightText.SelfModulate = COPYRIGHT_COLOR;
		}
    }

	private void QuitGame(){
		GetTree().Quit();
	}

	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		switch(choice){
			case 1: MenuScene.LoadMenu("PlayMenu"); break;
			case 2: MenuScene.LoadMenu("Settings/SettingsMenu"); break;
			case 3: MenuScene.LoadMenu("Credits/CreditsMenu"); break;
			case 4: QuitGame(); break;
		}
	}

	public override void MenuBack(){
		SFX.Play("Back");
		QuitGame();
	}
}