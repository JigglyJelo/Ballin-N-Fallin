using Godot;

public partial class ProfileMenu : ScrollableMenu{
    private const float LABEL_X_POS = -1920;
    private const float LABEL_SPACING = 200f;
    private PackedScene profileLabelScene = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn");

    private Keypad keypadPopup;
    private bool isKeypadOpen = false;

    public override void _Ready(){
        base._Ready(); 
        
        keypadPopup = GetNode<Keypad>("Keypad");
        keypadPopup.Visible = false;
        keypadPopup.OnTagConfirmed += HandleTagConfirmed;
        keypadPopup.OnCanceled += HandleKeypadCanceled;

        RefreshProfileList();
    }

    public override void _Process(double delta){
        if(isKeypadOpen) return;
        base._Process(delta);
    }

    private void RefreshProfileList(){
        if(selectionsContainer == null) return;

        foreach(Node child in selectionsContainer.GetChildren()){
            selectionsContainer.RemoveChild(child);
            child.QueueFree();
        }

        Selections = new Godot.Collections.Array<Node>();
        float currentY = -800f;

        Label createLabel = profileLabelScene.Instantiate<Label>();
        createLabel.Text = "+ Create New Profile";
        createLabel.Position = new Vector2(LABEL_X_POS, currentY);
        selectionsContainer.AddChild(createLabel);
        Selections.Add(createLabel);
        currentY += LABEL_SPACING;

        foreach(string profile in ControlProfileManager.Profiles){
            Label profileLabel = profileLabelScene.Instantiate<Label>();
            profileLabel.Text = profile;
            profileLabel.Position = new Vector2(LABEL_X_POS, currentY);
            selectionsContainer.AddChild(profileLabel);
            Selections.Add(profileLabel);
            currentY += LABEL_SPACING;
        }

        totalSelections = Selections.Count;
        if(Selection > totalSelections) Selection = totalSelections; 
        UpdateSelectionVisual();
    }

    protected override void MenuChoose(int choice){
        if(choice == 1){ 
            isKeypadOpen = true;
            if(selectionsContainer != null) selectionsContainer.Visible = false; 
            keypadPopup.Open(0); 
            SFX.Play("Confirm");
        }else{ 
            string selectedProfile = ControlProfileManager.Profiles[choice - 2];
            ControlsMenu.TargetProfile = selectedProfile; 
            SFX.Play("Confirm");
            MenuScene.LoadMenu("Settings/ControlsMenu"); 
        }
    }

    public override void MenuBack(){
        SFX.Play("Back");
        MenuScene.LoadMenu("Settings/SettingsMenu");
    }

    private void HandleTagConfirmed(string newTag){
        isKeypadOpen = false;
        keypadPopup.Close();
        if(selectionsContainer != null) selectionsContainer.Visible = true; 
        
        ControlProfileManager.CreateProfile(newTag); 
        
        SFX.Play("Confirm", 1.125f);
        RefreshProfileList();
    }

    private void HandleKeypadCanceled(){
        isKeypadOpen = false;
        keypadPopup.Close();
        if(selectionsContainer != null) selectionsContainer.Visible = true; 
        SFX.Play("Back", 1.125f);
    }

    public override void _Input(InputEvent @event){
        if(isKeypadOpen) return; 

        if(@event is InputEventJoypadButton btnEvent && btnEvent.IsPressed()){
            if(btnEvent.ButtonIndex == JoyButton.X){
                if(Selection > 1){ 
                    string selectedProfile = ControlProfileManager.Profiles[Selection - 2];
                    
                    if(selectedProfile != ControlProfileManager.DEFAULT_PROFILE){
                        ControlProfileManager.DeleteProfile(selectedProfile); 
                        SFX.Play("Move"); 
                        RefreshProfileList();
                        GetViewport().SetInputAsHandled();
                    }else{
                        SFX.Play("Error"); 
                    }
                }
            }
        }
    }
}