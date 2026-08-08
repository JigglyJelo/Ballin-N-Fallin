using Godot;
/// <summary>
/// An abstract menu class designed for grid-based layouts.
/// Automatically handles 2D wrapping so the selection loops around edges.
/// </summary>
public abstract partial class RectangularMenu : Menu{
	protected int rowCount = 3;
	protected int colCount = 3;

	protected override void InputChecks(double delta, int id){
		float fDelta = (float)delta;
		MouseInputs(fDelta);
		joystickTimer += fDelta;
		if(joystickTimer >= TIMEOUT){
			switch(GetInputDirection(id)){
				case InputDirection.Up: MenuUp(); break;
				case InputDirection.Down: MenuDown(); break;
				case InputDirection.Right: MenuRight(); break;
				case InputDirection.Left: MenuLeft(); break;
			}
		}
		if(Input.IsActionJustReleased("Charge N Launch" + id)) MenuChoose(Selection);
		else if(Input.IsActionJustReleased("B" + id)) MenuBack();
	}

	protected override void InputChecks(double delta){
		float fDelta = (float)delta;
		MouseInputs(fDelta);
		//Controllers
		if(joystickTimer >= TIMEOUT){
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				joystickTimer += fDelta / Game.MAX_PLAYERS;
				switch(GetInputDirection(i)){
					case InputDirection.Up: MenuUp(); break;
					case InputDirection.Down: MenuDown(); break;
					case InputDirection.Right: MenuRight(); break;
					case InputDirection.Left: MenuLeft(); break;
				}
				if(Input.IsActionJustReleased("Charge N Launch" + i)) MenuChoose(Selection);
				else if(Input.IsActionJustReleased("B" + i)) MenuBack();
			}
		}else{
			joystickTimer += fDelta;
		}
	}

    protected void MenuUp(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int row = (Selection - 1) / colCount; // Adjust for 1-based index
        row = (row - 1 + rowCount) % rowCount; // Move up cyclically
        Selection = row * colCount + ((Selection - 1) % colCount) + 1; // 1-based index
		joystickTimer = 0;
        UpdateSelectionVisual();
    }

    protected void MenuDown(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int row = (Selection - 1) / colCount; // Adjust for 1-based index
        row = (row + 1) % rowCount; // Move down cyclically
        Selection = row * colCount + ((Selection - 1) % colCount) + 1; // 1-based index
		joystickTimer = 0;
        UpdateSelectionVisual();
    }

    protected void MenuRight(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int col = (Selection - 1) % colCount; // Adjust for 1-based index
        col = (col + 1) % colCount; // Move right cyclically
        Selection = ((Selection - 1) / colCount) * colCount + col + 1; // 1-based index
		joystickTimer = 0;
        UpdateSelectionVisual();
    }

    protected void MenuLeft(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int col = (Selection - 1) % colCount; // Adjust for 1-based index
        col = (col - 1 + colCount) % colCount; // Move left cyclically
        Selection = ((Selection - 1) / colCount) * colCount + col + 1; // 1-based index
		joystickTimer = 0;
        UpdateSelectionVisual();
    }
}