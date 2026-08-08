using Godot;

public partial class PlayMenu : RectangularMenu{
	private Label localText,onlineText,subheaderText;
	private Polygon2D localButton,onlineButton;
	public override void _Ready(){
		rowCount = 1;
		colCount = 2;
		Selection = 1;
		base._Ready();
		localText = GetNode<Label>("Selections/LocalButton/LocalText");
		localButton = GetNode<Polygon2D>("Selections/LocalButton");
		onlineText = GetNode<Label>("Selections/OnlineButton/OnlineText");
		onlineButton = GetNode<Polygon2D>("Selections/OnlineButton");
		subheaderText = GetNode<Label>("SubHeaderText");
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		InputChecks(delta);
	}

	private void LoadMouseMenu(string nextMenu){
		MouseMenu.NextMenu = nextMenu;
		MenuScene.LoadMenu("MouseMenu");
	}

	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		switch(choice){
			case 1:
				if(Input.IsActionJustReleased("Charge N Launch Mouse")){
					LoadMouseMenu("PlayerMenu");
				}else{
					MenuScene.LoadMenu("PlayerMenu");
				}
				break;
			case 2:
				if(Input.IsActionJustReleased("Charge N Launch Mouse")){
					LoadMouseMenu("Online/OnlineMenu");
				}else{
					MenuScene.LoadMenu("Online/OnlineMenu");
				}
				break;
		}
	}

	public override void MenuBack(){
		SFX.Play("Back");
		MenuScene.LoadMenu("MainMenu");
	}

	protected override void UpdateSelectionVisual(){
		switch(Selection){
			case 1:
				//Selected
				localText.SelfModulate = SELECTED_COLOR;
				localButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				onlineText.SelfModulate = Colors.White;
				onlineButton.Color = BUTTON_COLOR;
				subheaderText.Text = "Play with people in the room.";
				break;
			case 2:
				//Selected
				onlineText.SelfModulate = SELECTED_COLOR;
				onlineButton.Color = SELECTED_BUTTON_COLOR;
				//Non-Selected
				localText.SelfModulate = Colors.White;
				localButton.Color = BUTTON_COLOR;
				subheaderText.Text = "Play with people online.";
				break;
		}
	}
}