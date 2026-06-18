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
    private const float MULTI_TAP_TIMEOUT = 0.75f;
    private int multiTapIndex = 0;
    private int lastPressX = -1;
    private int lastPressY = -1;
    
    private float inputCooldown = 0f;

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

    public override void _Input(InputEvent @event){
        if(!Visible || inputCooldown > 0) return;

        if(@event is InputEventKey keyEvent && keyEvent.Pressed){
            
            if(keyEvent.Keycode == Key.Backspace){
                if(currentTag.Length > 0){
                    currentTag = currentTag.Remove(currentTag.Length - 1);
                    ResetMultiTap();
                    UpdateSelectionVisual();
                    SFX.Play("Move");
                }
                GetViewport().SetInputAsHandled();
                return;
            }

            if(keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.KpEnter){
                ConfirmTag();
                GetViewport().SetInputAsHandled();
                return;
            }

            if(keyEvent.Unicode != 0){
                char typedChar = (char)keyEvent.Unicode;
                if(char.IsLetterOrDigit(typedChar) || typedChar == ' '){
                    if(currentTag.Length < Online.USERNAME_LENGTH){
                        currentTag += typedChar;
                        ResetMultiTap(); 
                        UpdateSelectionVisual();
                        SFX.Play("Confirm"); 
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
        inputCooldown = 0.1f;
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
            currentTag = currentTag.Remove(currentTag.Length - 1) + (isLowercase ? char.ToLower(lastChar) : char.ToUpper(lastChar));
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
        selectionX = (selectionX > 0) ? selectionX - 1 : 2;
        ResetMultiTap(); 
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuRight(){
        if(selectionY == 4) return; 
        selectionX = (selectionX < 2) ? selectionX + 1 : 0;
        ResetMultiTap();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuUp(){
        selectionY = (selectionY > 0) ? selectionY - 1 : 4;
        ResetMultiTap();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuDown(){
        selectionY = (selectionY < 4) ? selectionY + 1 : 0;
        ResetMultiTap();
        UpdateSelectionVisual();
        SFX.Play("Move");
    }

    protected override void MenuChoose(int selection){
        if(selectionY == 4){
            ConfirmTag(); 
            return;
        }

        string key = KEYPAD_CHARS[selectionY, selectionX];

        if(key == "DEL"){
            if(currentTag.Length > 0) currentTag = currentTag.Remove(currentTag.Length - 1);
            ResetMultiTap();
            UpdateSelectionVisual();
            SFX.Play("Move");
            return;
        }

        if(selectionX == lastPressX && selectionY == lastPressY && multiTapTimer > 0){
            multiTapIndex = (multiTapIndex + 1) % key.Length;
            currentTag = currentTag.Remove(currentTag.Length - 1); 
        }else{
            if(currentTag.Length >= Online.USERNAME_LENGTH){
                SFX.Play("Error");
                return;
            }
            multiTapIndex = 0;
            lastPressX = selectionX;
            lastPressY = selectionY;
        }

        char newChar = key[multiTapIndex];
        currentTag += isLowercase ? char.ToLower(newChar) : newChar;
        multiTapTimer = MULTI_TAP_TIMEOUT;
        
        SFX.Play("Confirm");
        UpdateSelectionVisual();
    }

    public override void MenuBack(){
        OnCanceled?.Invoke();
    }

    private void ConfirmTag(){
        if(currentTag.Length == 0){
            SFX.Play("Error"); 
            return;
        }

        if(ControlProfileManager.Profiles.Contains(currentTag)){
            SFX.Play("Error"); 
            return;
        }
        
        OnTagConfirmed?.Invoke(currentTag);
    }

    private void ResetMultiTap(){
        multiTapTimer = 0;
        lastPressX = -1;
        lastPressY = -1;
    }

    protected override void UpdateSelectionVisual(){
        tagLabel.Text = multiTapTimer > 0 ? currentTag + "_" : currentTag;

        for(int y = 0; y < 4; y++){
            for(int x = 0; x < 3; x++){
                bool isSelected = (selectionY == y && selectionX == x && selectionY != 4);
                keyLabels[y, x].SelfModulate = isSelected ? Colors.Green : Colors.White;
            }
        }

        confirmLabel.SelfModulate = (selectionY == 4) ? Colors.Green : Colors.White;
    }
}