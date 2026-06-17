using Godot;
/// <summary>
/// Abstract base class for UI menus. Handles switching between controller and mouse input, 
/// managing selection states, and calculating custom mouse hitboxes for standard Godot nodes.
/// </summary>
public abstract partial class Menu : Node2D{
    public static readonly Color SELECTED_COLOR = new Color(0,1,0);
    public static readonly Color SELECTED_BUTTON_COLOR = Color.Color8(255,216,134);
    public static readonly Color BUTTON_COLOR = Color.Color8(255,163,74);
    protected float defaultFontSize = 1;
    protected Vector2 joystickInput;
    /// <summary>The time in seconds to wait between accepting rapid mouse or stick inputs.</summary>
    public const float TIMEOUT = 0.2f;
    /// <summary>The deadzone threshold the joystick must pass to register a directional input.</summary>
    public const float STICK_THRESHOLD = 0.5f;
	protected float joystickTimer = TIMEOUT;
    private float mouseTimer = 0;
    /// <summary>The currently highlighted selection. Note: This is 1-indexed.</summary>
    public int Selection = 1;
    /// <summary>The interactive child nodes (Selections/Menu options) in this menu.</summary>
    public Godot.Collections.Array<Node> Selections = null;
    protected abstract void InputChecks(double delta);
    protected abstract void InputChecks(double delta, int id);
    public override void _Ready(){
        Node2D backButton = GetNodeOrNull<Node2D>("MenuBackButton");
        if(backButton != null) SetControllerUsage(!Cursor.UsingCursor);
    }
    public override void _Process(double delta){
		InputChecks(delta);
    }
    protected void MouseInputs(float fDelta){
        mouseTimer += fDelta;
        bool mouseActivated = Input.GetLastMouseVelocity() != Vector2.Zero || Input.IsActionJustPressed("Charge N Launch Mouse") || Input.IsActionJustReleased("Charge N Launch Mouse");
        if(mouseActivated && !Cursor.UsingCursor){
            SetControllerUsage(false);
        }
        if((Input.IsActionJustReleased("Charge N Launch Mouse") || (mouseTimer < TIMEOUT && mouseActivated) || mouseActivated) && Selections != null && Selections.Count > 0){
            for(int i = 0; i < Selections.Count; i++){
                switch(Selections[i]){
                    case Label label when IsMouseOverLabel(label):
                    case Sprite2D sprite when IsMouseOverSprite(sprite):
                    case Polygon2D polygon when IsMouseOverPolygon(polygon):
                        if(mouseTimer >= TIMEOUT){
                            if(Input.IsActionJustReleased("Charge N Launch Mouse")) mouseClicked(i);
                            if(mouseActivated) SelectionHovered(i);
                        }else{
                            SelectionHovered(i);
                        }
                        return;
                }
            }
        }else if(Selections == null || Selections.Count == 0) Selections = GetNodeOrNull("Selections").GetChildren();

        void mouseClicked(int index){
            Selections = null;
            mouseTimer = 0;
            MenuChoose(index+1);
        }
    }

    public static bool IsMouseOverLabel(Label label){
        Vector2 mousePosition = label.GetGlobalMousePosition();
        float xSize = Mathf.Abs((label.Size.X/2f)*label.Scale.X);
	    float ySize = Mathf.Abs((label.Size.Y/2f)*label.Scale.Y);
        float xDist = Mathf.Abs((label.GlobalPosition.X + xSize) - mousePosition.X);
	    float yDist = Mathf.Abs((label.GlobalPosition.Y + ySize) - mousePosition.Y);
        return xDist < xSize && yDist < ySize;
    }
    public static bool IsMouseOverSprite(Sprite2D sprite){
        Vector2 mousePosition = sprite.GetGlobalMousePosition();
        float halfWidth = (sprite.Texture.GetWidth() * sprite.Scale.X) / 2f;
        float halfHeight = (sprite.Texture.GetHeight() * sprite.Scale.Y) / 2f;
        Vector2 spritePosition = sprite.GlobalPosition;
        float xDist = Mathf.Abs(mousePosition.X - spritePosition.X);
        float yDist = Mathf.Abs(mousePosition.Y - spritePosition.Y);
        return xDist < halfWidth && yDist < halfHeight;
    }
    public static bool IsMouseOverPolygon(Polygon2D polygon){
        return Geometry2D.IsPointInPolygon(polygon.GetLocalMousePosition(),polygon.Polygon);
    }

    protected void SelectionHovered(int index){
        if(Selections != null && Selections[index] is Node && !Selections[index].HasMeta("nonclickable")) Cursor.CursorThisFrame = Input.CursorShape.PointingHand;

        if(Selection != index+1){
            Selection = index+1;
            UpdateSelectionVisual();
            SFX.Play("Move");
        }
    }
    protected abstract void UpdateSelectionVisual();
    protected abstract void MenuChoose(int choice);
    public abstract void MenuBack();
    protected InputDirection GetInputDirection(int controllerId){
        if(controllerId != (int)PlayerData.PlayerInputDevice.Mouse){
            joystickInput = Input.GetVector("Aim Left" + controllerId,"Aim Right" + controllerId,"Aim Up" + controllerId,"Aim Down" + controllerId);
            if(joystickInput.IsZeroApprox()) joystickInput = Input.GetVector("DPad Left" + controllerId,"DPad Right" + controllerId,"DPad Up" + controllerId,"DPad Down" + controllerId);
        }else{
            joystickInput = Input.GetVector("Left Keyboard", "Right Keyboard", "Up Keyboard", "Down Keyboard");
        }
        
        //Vertical & Neutral
        if(joystickInput.X == 0 && joystickInput.Y == 0) return InputDirection.Neutral;
        else if(joystickInput.Y <= -STICK_THRESHOLD) return InputDirection.Up;
        else if(joystickInput.Y >= STICK_THRESHOLD) return InputDirection.Down;
        else if(joystickInput.X >= STICK_THRESHOLD) return InputDirection.Right;
        else if(joystickInput.X <= -STICK_THRESHOLD) return InputDirection.Left;
        else return InputDirection.Neutral;
    }
    protected enum InputDirection{
        Up,Down,Right,Left,Neutral
    }
    protected void SetControllerUsage(bool usingController){
        if(usingController){
            Cursor.UsingCursor = false;
            Node2D backButton = GetNodeOrNull<Node2D>("MenuBackButton");
            if(backButton != null) backButton.Modulate = new Color(0,0,0,float.Epsilon); //Hack needed cause gdscript add on used for aa lines because godot's implementation doesnt work
            Input.MouseMode = Input.MouseModeEnum.Hidden;
        }else{
            Cursor.UsingCursor = true;
            Node2D backButton = GetNodeOrNull<Node2D>("MenuBackButton");
            if(backButton != null) backButton.Modulate = new Color(1,1,1,1);
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
}