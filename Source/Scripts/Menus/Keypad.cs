using Godot;
using System;

public partial class Keypad : Menu2D{
    public Action<string> OnTagConfirmed;
    public Action OnCanceled;
    public int InputId = 0;

    private int selectionX = 0;
    private int selectionY = 0;
    private string currentTag = "";
    private bool isLowercase = false;

    private float multiTapTimer = 0f;
    private const float MULTI_TAP_TIMEOUT = 1;
    private int multiTapIndex = 0;
    private int lastPressX = -1;
    private int lastPressY = -1;
    
    private float inputCooldown = 0f; // Prevents the open button press from bleeding through

    private Label tagLabel;
    private Label[,] keyLabels = new Label[4, 3];
    private Label confirmLabel;

    private readonly string[,] KEYPAD_CHARS = new string[4, 3]{
        {"123", "ABC", "DEF"},
        {"GHI", "JKL", "MNO"},
        {"PQRS", "TUV", "WXYZ"},
        {"456", "7890 ", "DEL"}
    };

    public override void _Ready(){
        base._Ready();
        tagLabel = GetNode<Label>("TagLabel");
        confirmLabel = GetNode<Label>("KeypadBackground/Keys/Confirm");

        Node keysContainer = GetNode("KeypadBackground/Keys");
        int childIndex = 0;
        for(int y = 0; y < 4; y++){
            for(int x = 0; x < 3; x++){
                keyLabels[y, x] = keysContainer.GetChild<Label>(childIndex);
                childIndex++;
            }
        }
    }

    public override void _Process(double delta){
        if(!Visible) return;

        // Block inputs for a split second after opening
        if(inputCooldown > 0){
            inputCooldown -= (float)delta;
            return;
        }

        InputChecks(delta, InputId);

        if(Input.IsActionJustReleased("Y" + InputId)){
            ToggleCase();
        }

        if(multiTapTimer > 0){
            multiTapTimer -= (float)delta;
            if(multiTapTimer <= 0) ResetMultiTap();
        }
    }

    // NEW: Listen for physical keyboard input
    public override void _Input(InputEvent @event){
        if (!Visible || inputCooldown > 0) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed){
            
            // Handle Backspace
            if (keyEvent.Keycode == Key.Backspace){
                if (currentTag.Length > 0){
                    currentTag = currentTag.Substring(0, currentTag.Length - 1);
                    ResetMultiTap();
                    UpdateSelectionVisual();
                    SFX.Play("Move");
                }
                GetViewport().SetInputAsHandled();
                return;
            }

            // Handle Enter/Return to confirm
            if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter){
                ConfirmTag();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Handle typing standard characters via Unicode
            if (keyEvent.Unicode != 0){
                char typedChar = (char)keyEvent.Unicode;

                // Ensure the character is valid (alphanumeric or space)
                if (char.IsLetterOrDigit(typedChar) || typedChar == ' '){
                    if (currentTag.Length < Online.USERNAME_LENGTH){
                        currentTag += typedChar;
                        ResetMultiTap(); // Stop multi-tap interference if they start typing physically
                        UpdateSelectionVisual();
                        SFX.Play("Confirm"); // Using Confirm SFX for typing stroke, adjust as needed
                    }else{
                        SFX.Play("Error");
                    }
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    public void Open(int controllerId = 0){
        InputId = controllerId;
        Visible = true;
        currentTag = "";
        selectionX = 0;
        selectionY = 0;
        isLowercase = false;
        inputCooldown = 0.1f; // Set a 0.1 second delay before accepting inputs
        ResetMultiTap();
        UpdateLabels();
        UpdateSelectionVisual();
    }

    public void Close(){
        Visible = false;
    }

    private void ToggleCase(){
        isLowercase = !isLowercase;

        if(multiTapTimer > 0 && currentTag.Length > 0){
            char lastChar = currentTag[currentTag.Length - 1];
            string toggledChar = isLowercase ? lastChar.ToString().ToLower() : lastChar.ToString().ToUpper();
            currentTag = currentTag.Substring(0, currentTag.Length - 1) + toggledChar;
        }

        UpdateLabels();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    private void UpdateLabels(){
        for(int y = 0; y < 4; y++){
            for(int x = 0; x < 3; x++){
                if(KEYPAD_CHARS[y, x] == "DEL") continue; 
                keyLabels[y, x].Text = isLowercase ? KEYPAD_CHARS[y, x].ToLower() : KEYPAD_CHARS[y, x];
            }
        }
    }

    protected override void MenuLeft(){
        if(selectionY == 4) return; 

        if(selectionX > 0) selectionX--;
        else selectionX = 2;
        ResetMultiTap(); 
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuRight(){
        if(selectionY == 4) return; 

        if(selectionX < 2) selectionX++;
        else selectionX = 0; 
        ResetMultiTap();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuUp(){
        if(selectionY > 0) selectionY--;
        else selectionY = 4; 
        ResetMultiTap();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuDown(){
        if(selectionY < 4) selectionY++;
        else selectionY = 0; 
        ResetMultiTap();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuChoose(int selection){
        if(selectionY == 4){
            ConfirmTag(); // UPDATED: Calls the extracted method
            return;
        }

        string key = KEYPAD_CHARS[selectionY, selectionX];

        if(key == "DEL"){
            if(currentTag.Length > 0){
                currentTag = currentTag.Substring(0, currentTag.Length - 1);
            }
            ResetMultiTap();
            UpdateSelectionVisual();
            SFX.Play("Move");
            return;
        }

        if(selectionX == lastPressX && selectionY == lastPressY && multiTapTimer > 0){
            multiTapIndex = (multiTapIndex + 1) % key.Length;
            string newChar = isLowercase ? key[multiTapIndex].ToString().ToLower() : key[multiTapIndex].ToString();
            currentTag = currentTag.Substring(0, currentTag.Length - 1) + newChar;
            multiTapTimer = MULTI_TAP_TIMEOUT; 
        }else{
            if(currentTag.Length < Online.USERNAME_LENGTH){
                multiTapIndex = 0;
                string newChar = isLowercase ? key[multiTapIndex].ToString().ToLower() : key[multiTapIndex].ToString();
                currentTag += newChar;
                lastPressX = selectionX;
                lastPressY = selectionY;
                multiTapTimer = MULTI_TAP_TIMEOUT;
            }else{
                SFX.Play("Error");
                return;
            }
        }
        SFX.Play("Confirm");
        UpdateSelectionVisual();
    }

    public override void MenuBack(){
        OnCanceled?.Invoke();
    }

    // NEW: Extracted confirmation logic so both Virtual Keypad and Physical Enter key can use it
    private void ConfirmTag(){
        if(currentTag.Length > 0){
            if(ControlProfileManager.Profiles.Contains(currentTag)){
                SFX.Play("Error"); // Reject the input, name is taken
                return;
            }
            
            OnTagConfirmed?.Invoke(currentTag);
        }else{
            SFX.Play("Error"); // Reject empty strings
        }
    }

    private void ResetMultiTap(){
        multiTapTimer = 0;
        lastPressX = -1;
        lastPressY = -1;
    }

    protected override void UpdateSelectionVisual(){
        tagLabel.Text = currentTag;
        if(multiTapTimer > 0) tagLabel.Text += "_";

        for(int y = 0; y < 4; y++){
            for(int x = 0; x < 3; x++){
                if(selectionY == y && selectionX == x && selectionY != 4){
                    keyLabels[y, x].SelfModulate = Colors.Green;
                }else{
                    keyLabels[y, x].SelfModulate = Colors.White;
                }
            }
        }

        if(selectionY == 4) confirmLabel.SelfModulate = Colors.Green;
        else confirmLabel.SelfModulate = Colors.White;
    }
}