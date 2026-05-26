using Godot;

public abstract partial class VerticalMenu : Menu{
    protected int totalSelections;
    private bool hasLeftRight = false;
    private ILeftRightSelections leftRightCast;

    public override void _Ready(){
        base._Ready();
        hasLeftRight = this as ILeftRightSelections != null;
        if(hasLeftRight) leftRightCast = this as ILeftRightSelections;
    }

    protected override void InputChecks(double delta,int id){
        float fDelta = (float)delta;
        MouseInputs(fDelta);
        if(id >= (int)PlayerData.PlayerInputDevice.Mouse) return;
        joystickTimer += fDelta;
        switch(GetInputDirection(id)){
            case InputDirection.Up when joystickTimer >= TIMEOUT:
                MenuUp();
                break;
            case InputDirection.Down when joystickTimer >= TIMEOUT:
                MenuDown();
                break;
            case InputDirection.Right when hasLeftRight:
                SetControllerUsage(true);
                if(ILeftRightSelections.HoldCheck()) leftRightCast.MenuRight();
                ILeftRightSelections.UpdateHold(fDelta);
                break;
            case InputDirection.Left when hasLeftRight:
                SetControllerUsage(true);
                if(ILeftRightSelections.HoldCheck()) leftRightCast.MenuLeft();
                ILeftRightSelections.UpdateHold(fDelta);
                break;
            case InputDirection.Neutral when hasLeftRight && !Cursor.UsingCursor:
                ILeftRightSelections.ResetHold();
                break;
        }
        if(Input.IsActionJustReleased("Charge N Launch" + id)) MenuChoose(Selection);
        else if(Input.IsActionJustReleased("B" + id)) MenuBack();
    }
    protected override void InputChecks(double delta){
        float fDelta = (float)delta;
        MouseInputs(fDelta);
        int neutralStickCount = 0;
        //Controllers
        for(int i = 0; i < Game.MAX_PLAYERS; i++){
            joystickTimer += fDelta / Game.MAX_PLAYERS;
            switch(GetInputDirection(i)){
                case InputDirection.Up when joystickTimer >= TIMEOUT:
                    MenuUp();
                    break;
                case InputDirection.Down when joystickTimer >= TIMEOUT:
                    MenuDown();
                    break;
                case InputDirection.Right when hasLeftRight:
                    SetControllerUsage(true);
                    if(ILeftRightSelections.HoldCheck()) leftRightCast.MenuRight();
                    ILeftRightSelections.UpdateHold(fDelta);
                    break;
                case InputDirection.Left when hasLeftRight:
                    SetControllerUsage(true);
                    if(ILeftRightSelections.HoldCheck()) leftRightCast.MenuLeft();
                    ILeftRightSelections.UpdateHold(fDelta);
                    break;
                case InputDirection.Neutral when hasLeftRight && !Cursor.UsingCursor:
                    neutralStickCount++;
                    break;
            }
            if(!Cursor.UsingCursor && neutralStickCount == Game.MAX_PLAYERS) ILeftRightSelections.ResetHold();
            if(Input.IsActionJustReleased("Charge N Launch" + i)) MenuChoose(Selection);
            else if(Input.IsActionJustReleased("B" + i)) MenuBack();
        }
    }

    protected void MenuUp(){
        SetControllerUsage(true);
        if(Selection > 1) Selection--;
		else Selection = totalSelections;
		UpdateSelectionVisual();
		joystickTimer = 0;
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
    }

    protected void MenuDown(){
        SetControllerUsage(true);
		if(Selection < totalSelections) Selection++;
		else Selection = 1;
		UpdateSelectionVisual();
		joystickTimer = 0;
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
	}

    protected override void UpdateSelectionVisual(){
        //Get Selections list
        if(Selections == null) Selections = GetNode("Selections").GetChildren();
        else if(Selections.Count < 1) return;
        //Set colors and size for all options
        for(int i = 0; i < Selections.Count; i++){
            Node selectionOption = Selections[i];
            if(selectionOption is Label textOption){
                const float SIZE = 1 + (1/3f);
                if(Selection == i+1){ //Current Selection becomes green and grows
                    float bigFontScale;
                    try{
					    bigFontScale = int.Parse(textOption.Name.ToString().Substring(textOption.Name.ToString().Length-3))*SIZE/100f;
				    }catch{
					    bigFontScale = defaultFontSize*SIZE;
				    }
                    textOption.SelfModulate = SELECTED_COLOR;
                    textOption.ZIndex = 1;
                    Tween scaleTween = CreateTween();
                    float scaleSize = bigFontScale;
                    scaleTween.TweenProperty(textOption,"scale",new Vector2(scaleSize,scaleSize),0.15f);
                }else{ //Previous selection becomes white and shrinks
                    float normalFontScale;
                    try{
					    normalFontScale = int.Parse(textOption.Name.ToString().Substring(textOption.Name.ToString().Length-3))/100f;
				    }catch{
					    normalFontScale = defaultFontSize;
				    }
                    textOption.SelfModulate = Colors.White;
                    textOption.ZIndex = 0;
                    Tween scaleTween = CreateTween();
                    scaleTween.TweenProperty(textOption,"scale",new Vector2(normalFontScale,normalFontScale),0.15f);
                }
            }
        }
    }
}