using Godot;

public partial class MouseMenu : Polygon2D{
	private Polygon2D buttonA, buttonB;
	private Label infoText;
	private Node2D backButton;
	private int selection = 0;
	private Sprite2D cursorPupils, cursorArrow, directionPupils, directionArrow;
	private Vector2 lastMousePosition;
	public static string NextMenu;
	private static readonly Color BUTTON_COLOR = Color.Color8(243,114,47);
    private static readonly Color SELECTED_BUTTON_COLOR = Color.Color8(251,148,100);
	public override void _Ready(){
		buttonA = GetNode<Polygon2D>("ButtonA");
		buttonB = GetNode<Polygon2D>("ButtonB");
		infoText = GetNode<Label>("InfoText");
		backButton = GetNode<Node2D>("MenuBackButton");
		cursorPupils = GetNode<Sprite2D>("ButtonA/Player/Eyes/Pupils");
		directionPupils = GetNode<Sprite2D>("ButtonB/Player/Eyes/Pupils");
		cursorArrow = GetNode<Sprite2D>("ButtonA/Player/Arrow");
		directionArrow = GetNode<Sprite2D>("ButtonB/Player/Arrow");
	}

	public override void _Process(double delta){
		if(Input.GetLastMouseVelocity() != Vector2.Zero){
			if(Geometry2D.IsPointInPolygon(buttonA.GetLocalMousePosition(),buttonA.Polygon)){
				if(selection != 1){
					selection = 1;
					cursorArrow.Visible = true;
					UpdateInfoText(selection);
				}
			}else if(Geometry2D.IsPointInPolygon(buttonB.GetLocalMousePosition(),buttonB.Polygon)){
				if(selection != 2){
					selection = 2;
					directionArrow.Visible = true;
					UpdateInfoText(selection);
				}
			}else if(overBackButton()){
				if(selection != 3){
					selection = 3;
					UpdateInfoText(selection);
				}
			}else{
				if(selection != 0){
					selection = 0;
					cursorPupils.Position = Vector2.Zero;
					directionPupils.Position = Vector2.Zero;
					cursorArrow.Visible = false;
					directionArrow.Visible = false;
					UpdateInfoText(selection);
				}
			}
		}
		if(Input.IsActionJustReleased("Charge N Launch Mouse")){
			MenuChoose(selection);
		}
		Aim();

		bool overBackButton(){
			Vector2 mousePosition = backButton.GetLocalMousePosition();
			return mousePosition.X > MenuBackButton.LEFT && mousePosition.X < MenuBackButton.RIGHT && mousePosition.Y > MenuBackButton.TOP && mousePosition.Y < MenuBackButton.BOTTOM;
		}
	}

	private void Aim(){
		switch(selection){
			case 1:
				Vector2 cursorAngle = cursorPupils.GlobalPosition.DirectionTo(GetGlobalMousePosition());
				cursorPupils.Position = cursorAngle * 6;
				cursorArrow.Rotation = cursorAngle.Angle();
				cursorArrow.Position = cursorAngle * 125;
				break;
			case 2:
				if(lastMousePosition == Vector2.Zero){
					lastMousePosition = GetGlobalMousePosition();
					return;
				}else if(lastMousePosition.DistanceSquaredTo(GetGlobalMousePosition()) < 18 * 18){
					return;
				}
				Vector2 directionAngle = lastMousePosition.DirectionTo(GetGlobalMousePosition());
				directionPupils.Position = directionAngle * 6;
				directionArrow.Rotation = directionAngle.Angle();
				directionArrow.Position = directionAngle * 125;
				lastMousePosition = GetGlobalMousePosition();
				break;
		}
	}

	private void UpdateInfoText(int selection){
		switch(selection){
			case 0: 
				infoText.Text = "Hover over a type to learn more";
				buttonA.Color = BUTTON_COLOR;
				buttonB.Color = BUTTON_COLOR;
				backButton.Modulate = Colors.White;
				break;
			case 1: 
				infoText.Text = "Aim towards the mouse cursor";
				buttonA.Color = SELECTED_BUTTON_COLOR;
				buttonB.Color = BUTTON_COLOR;
				backButton.Modulate = Colors.White;
				break;
			case 2: 
				infoText.Text = "Aim towards the direction you move the mouse";
				buttonA.Color = BUTTON_COLOR;
				buttonB.Color = SELECTED_BUTTON_COLOR;
				backButton.Modulate = Colors.White;
				break;
			case 3:
				infoText.Text = "Hover over a type to learn more";
				buttonA.Color = BUTTON_COLOR;
				buttonB.Color = BUTTON_COLOR;
				backButton.Modulate = Menu.SELECTED_COLOR;
				break;
		}
	}

	private void MenuBack(){
		SFX.Play("Back");
		MenuScene.LoadMenu("MainMenu");
	}

	private void MenuChoose(int selection){
		switch(selection){
			case 1: 
				Game.MouseMode = Game.MouseModeEnum.Cursor;
				loadNextMenu();
				break;
			case 2: 
				Game.MouseMode = Game.MouseModeEnum.Direction;
				loadNextMenu();
				break;
			case 3:
				Game.MouseMode = Game.MouseModeEnum.Off;
				MenuBack();
				break;
		}

		void loadNextMenu(){
			SFX.Play("Confirm");
			Online.InputId = PlayerData.PlayerInputDevice.Mouse;
			MenuScene.LoadMenu(NextMenu);
		}
	}
}
