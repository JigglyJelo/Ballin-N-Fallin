using Godot;

public partial class ProfileMenu : ScrollableMenu{
    private const float LABEL_X_POS = -1920;
    private const float LABEL_SPACING = 200f;
	private PackedScene profileLabelScene = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn");

    public override void _Ready(){
        base._Ready(); 
        RefreshProfileList();
    }

    private void RefreshProfileList(){
        if(selectionsContainer == null) return;

        //Clear existing children for a clean refresh
        foreach(Node child in selectionsContainer.GetChildren()){
            selectionsContainer.RemoveChild(child);
            child.QueueFree();
        }

        Selections = new Godot.Collections.Array<Node>();
        float currentY = -800f;

        //Add "Create Profile" at the very top
        Label createLabel = profileLabelScene.Instantiate<Label>();
		createLabel.Text = "+ Create New Profile";
		createLabel.Position = new Vector2(LABEL_X_POS, currentY);
        selectionsContainer.AddChild(createLabel);
        Selections.Add(createLabel);
        currentY += LABEL_SPACING;

        //Populate the list directly from your manager's Profiles list
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
            //1 is CREATE NEW PROFILE
            ControlProfileManager.CreateAutoNamedProfile();
            SFX.Play("Confirm");
            RefreshProfileList();
        }else{ 
            //Anything else is selecting an existing profile to edit
            //choice - 2 accounts for 1-based index and the Create button
            string selectedProfile = ControlProfileManager.Profiles[choice - 2];
            
            //Tell the ControlsMenu which profile we want to map
            ControlsMenu.TargetProfile = selectedProfile; 
            
            SFX.Play("Confirm");
            MenuScene.LoadMenu("Settings/ControlsMenu"); 
        }
    }

    public override void MenuBack(){
        SFX.Play("Back");
        MenuScene.LoadMenu("Settings/SettingsMenu");
    }

    public override void _Input(InputEvent @event){
        //Profile Deletion Logic (Press X)
        if(@event is InputEventJoypadButton btnEvent && btnEvent.IsPressed()){
            if(btnEvent.ButtonIndex == JoyButton.X){
                if(Selection > 1){ //Ensure we don't try to delete the "+ Create" button
                    string selectedProfile = ControlProfileManager.Profiles[Selection - 2];
                    
                    if(selectedProfile != ControlProfileManager.DEFAULT_PROFILE){
                        ControlProfileManager.DeleteProfile(selectedProfile); 
                        SFX.Play("Move"); 
                        RefreshProfileList();
                        GetViewport().SetInputAsHandled();
                    }else{
                        SFX.Play("Error"); //Cannot delete "Default"
                    }
                }
            }
        }
    }
}