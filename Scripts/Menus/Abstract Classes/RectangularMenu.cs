using Godot;

public abstract partial class RectangularMenu : Menu{
    protected int rowCount = 3;  // Default row count
    protected int colCount = 3;  // Default column count

    protected override void InputChecks(double delta, int id){
        float fDelta = (float)delta;
        MouseInputs(fDelta);
        joystickTimer += fDelta;
        if(joystickTimer >= TIMEOUT){
            switch(GetInputDirection(id)){
                case InputDirection.Up: MenuUp(); joystickTimer = 0; break;
                case InputDirection.Down: MenuDown(); joystickTimer = 0; break;
                case InputDirection.Right: MenuRight(); joystickTimer = 0; break;
                case InputDirection.Left: MenuLeft(); joystickTimer = 0; break;
            }
        }
        if(Input.IsActionJustReleased("Charge N Launch" + id)) MenuChoose(Selection);
        else if(Input.IsActionJustReleased("B" + id)) MenuBack();
    }

    protected override void InputChecks(double delta){
        float fDelta = (float)delta;
        MouseInputs(fDelta);
        //Controllers
        for(int i = 1; i <= Game.MAX_PLAYERS; i++){
            joystickTimer += fDelta / Game.MAX_PLAYERS;
            switch(GetInputDirection(i)){
                case InputDirection.Up: MenuUp(); joystickTimer = 0; break;
                case InputDirection.Down: MenuDown(); joystickTimer = 0; break;
                case InputDirection.Right: MenuRight(); joystickTimer = 0; break;
                case InputDirection.Left: MenuLeft(); joystickTimer = 0; break;
            }
            if(Input.IsActionJustReleased("Charge N Launch" + i)) MenuChoose(Selection);
            else if(Input.IsActionJustReleased("B" + i)) MenuBack();
        }
    }

    protected void MenuUp(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int row = (Selection - 1) / colCount; // Adjust for 1-based index
        row = (row - 1 + rowCount) % rowCount; // Move up cyclically
        Selection = row * colCount + ((Selection - 1) % colCount) + 1; // 1-based index
        UpdateSelectionVisual();
    }

    protected void MenuDown(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int row = (Selection - 1) / colCount; // Adjust for 1-based index
        row = (row + 1) % rowCount; // Move down cyclically
        Selection = row * colCount + ((Selection - 1) % colCount) + 1; // 1-based index
        UpdateSelectionVisual();
    }

    protected void MenuRight(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int col = (Selection - 1) % colCount; // Adjust for 1-based index
        col = (col + 1) % colCount; // Move right cyclically
        Selection = ((Selection - 1) / colCount) * colCount + col + 1; // 1-based index
        UpdateSelectionVisual();
    }

    protected void MenuLeft(){
        SetControllerUsage(true);
        SFX.Play("Move", Game.Random.Next(80,110) / 100f);
        int col = (Selection - 1) % colCount; // Adjust for 1-based index
        col = (col - 1 + colCount) % colCount; // Move left cyclically
        Selection = ((Selection - 1) / colCount) * colCount + col + 1; // 1-based index
        UpdateSelectionVisual();
    }
}