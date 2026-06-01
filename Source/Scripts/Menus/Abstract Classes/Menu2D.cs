using Godot;

public abstract partial class Menu2D : Menu{
    protected override void InputChecks(double delta,int id){
        float fDelta = (float)delta;
        MouseInputs(fDelta);
        joystickTimer += fDelta;
        if(joystickTimer >= TIMEOUT){
            switch(GetInputDirection(id)){
                case InputDirection.Up: SetControllerUsage(true); MenuUp(); joystickTimer = 0; break;
                case InputDirection.Down: SetControllerUsage(true); MenuDown(); joystickTimer = 0; break;
                case InputDirection.Right: SetControllerUsage(true); MenuRight(); joystickTimer = 0; break;
                case InputDirection.Left: SetControllerUsage(true); MenuLeft(); joystickTimer = 0; break;
            }
        }
        if(Input.IsActionJustReleased("Charge N Launch" + id)) MenuChoose(Selection);
        else if(Input.IsActionJustReleased("B" + id)) MenuBack();
    }

    protected override void InputChecks(double delta){
        float fDelta = (float)delta;
        MouseInputs(fDelta);
        //Controllers
        for(int i = 0; i < Game.MAX_PLAYERS; i++){
            joystickTimer +=  fDelta / Game.MAX_PLAYERS;
            if(joystickTimer >= TIMEOUT){
                switch(GetInputDirection(i)){
                    case InputDirection.Up: SetControllerUsage(true); MenuUp(); joystickTimer = 0; break;
                    case InputDirection.Down: SetControllerUsage(true); MenuDown(); joystickTimer = 0; break;
                    case InputDirection.Right: SetControllerUsage(true); MenuRight(); joystickTimer = 0; break;
                    case InputDirection.Left: SetControllerUsage(true); MenuLeft(); joystickTimer = 0; break;
                }
            }
            if(Input.IsActionJustReleased("Charge N Launch" + i)) MenuChoose(Selection);
            else if(Input.IsActionJustReleased("B" + i)) MenuBack();
        }
    }

    protected abstract void MenuUp();
    protected abstract void MenuDown();
    protected abstract void MenuLeft();
    protected abstract void MenuRight();
}