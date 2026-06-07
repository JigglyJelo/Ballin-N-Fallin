using Godot;
using System.Collections.Generic;

public partial class ControlsMenu : VerticalMenu, ILeftRightSelections{
    private Label[] actionLabels;
    private Label profileSelectorLabel;
    private Label vibrationLabel;
    private Label createProfileLabel;
    private int currentProfileIndex = 0;
    private bool isListening = false;
    private float remapCooldown = 0f;

    public override void _Ready(){
        base._Ready();
        
        // 1 (Create) + 1 (Selector) + 4 (Actions) + 1 (Vibration)
        totalSelections = ControlProfileManager.REMAP_ACTIONS.Length + 3; 
        Selection = 1;
        
        profileSelectorLabel = GetNode<Label>("Selections/ProfileSelector");
        vibrationLabel = GetNode<Label>("Selections/VibrationLabel");
        createProfileLabel = GetNode<Label>("Selections/CreateProfileLabel");
        
        actionLabels = new Label[ControlProfileManager.REMAP_ACTIONS.Length];
        for(int i = 0; i < actionLabels.Length; i++){
            actionLabels[i] = GetNode<Label>($"Selections/ActionLabel_{i}");
        }

        UpdateSelectionVisual();
        UpdateTexts();
    }

    public override void _Process(double delta){
        if(remapCooldown > 0){
            remapCooldown -= (float)delta;
            return;
        }

        if(isListening) return;
        base._Process(delta);
    }

    protected override void MenuChoose(int choice){
        if(choice == 2){ // 2 is now the Profile Selector
            SFX.Play("Error"); 
            return;
        }

        string activeProfile = ControlProfileManager.Profiles[currentProfileIndex];
        if(choice == 1){ // 1 is CREATE NEW PROFILE
            string newProfileName = ControlProfileManager.CreateAutoNamedProfile();
            currentProfileIndex = ControlProfileManager.Profiles.IndexOf(newProfileName);
            SFX.Play("Confirm");
            UpdateTexts();
            return;
        }

        if(activeProfile == ControlProfileManager.DEFAULT_PROFILE){
            SFX.Play("Error");
            return;
        }

        if(choice == totalSelections){ // totalSelections (7) is VIBRATION TOGGLE
            bool currentVibration = ControlProfileManager.GetVibration(activeProfile);
            ControlProfileManager.SetVibration(activeProfile, !currentVibration);
            SFX.Play("Confirm");
            UpdateTexts();
            return;
        }

        SFX.Play("Confirm");
        isListening = true;
        
        //Actions start at choice 3, so we subtract 3 to get index 0
        int actionIndex = choice - 3;
        actionLabels[actionIndex].Text = ControlProfileManager.REMAP_ACTIONS[actionIndex] + ": [Press Button to Add]";
    }

    public override void MenuBack(){
        SFX.Play("Back");
        MenuScene.LoadMenu("Settings/SettingsMenu");
        QueueFree();
    }

    public void MenuRight(){
        string activeProfile = ControlProfileManager.Profiles[currentProfileIndex];

        if(Selection == 2){ // 2 is Profile Selector
            SFX.Play("Move", Game.Random.Next(80, 110) / 100f);
            if(currentProfileIndex < ControlProfileManager.Profiles.Count - 1) currentProfileIndex++;
            else currentProfileIndex = 0; 
            joystickTimer = 0;
            UpdateTexts();
        }else if(Selection == totalSelections){ // totalSelections is Vibration Toggle
            if(activeProfile == ControlProfileManager.DEFAULT_PROFILE){
                SFX.Play("Error");
                return;
            }
            bool currentVibration = ControlProfileManager.GetVibration(activeProfile);
            ControlProfileManager.SetVibration(activeProfile, !currentVibration);
            SFX.Play("Move", Game.Random.Next(80, 110) / 100f);
            joystickTimer = 0;
            UpdateTexts();
        }
    }

    public void MenuLeft(){
        string activeProfile = ControlProfileManager.Profiles[currentProfileIndex];

        if(Selection == 2){ // 2 is Profile Selector
            SFX.Play("Move", Game.Random.Next(80, 110) / 100f);
            if(currentProfileIndex > 0) currentProfileIndex--;
            else currentProfileIndex = ControlProfileManager.Profiles.Count - 1; 
            joystickTimer = 0;
            UpdateTexts();
        }else if(Selection == totalSelections){ // totalSelections is Vibration Toggle
            if(activeProfile == ControlProfileManager.DEFAULT_PROFILE){
                SFX.Play("Error");
                return;
            }
            bool currentVibration = ControlProfileManager.GetVibration(activeProfile);
            ControlProfileManager.SetVibration(activeProfile, !currentVibration);
            SFX.Play("Move", Game.Random.Next(80, 110) / 100f);
            joystickTimer = 0;
            UpdateTexts();
        }
    }

    public override void _Input(InputEvent @event){
        if(!isListening){
            // Ensure we are strictly on the Actions (Selection 3, 4, 5, 6)
            if(Selection > 2 && Selection < totalSelections && @event is InputEventJoypadButton btnEvent && btnEvent.IsPressed()){
                string activeProfile = ControlProfileManager.Profiles[currentProfileIndex];
                
                if(activeProfile == ControlProfileManager.DEFAULT_PROFILE && (btnEvent.ButtonIndex == JoyButton.X || btnEvent.ButtonIndex == JoyButton.Y)){
                    SFX.Play("Error");
                    GetViewport().SetInputAsHandled();
                    return;
                }

                int actionIndex = Selection - 3;
                string targetAction = ControlProfileManager.REMAP_ACTIONS[actionIndex];

                if(btnEvent.ButtonIndex == JoyButton.X){
                    ControlProfileManager.ClearEventsFromProfile(activeProfile, targetAction);
                    SFX.Play("Move"); 
                    UpdateTexts();
                    GetViewport().SetInputAsHandled();
                }else if(btnEvent.ButtonIndex == JoyButton.Y){
                    ControlProfileManager.RestoreFactoryDefault(activeProfile, targetAction);
                    SFX.Play("Confirm");
                    UpdateTexts();
                    GetViewport().SetInputAsHandled();
                }
            }
            return;
        }

        if(@event is InputEventJoypadButton joyEvent && joyEvent.IsPressed()){
            if(joyEvent.ButtonIndex == JoyButton.Start || joyEvent.ButtonIndex == JoyButton.Back){
                SFX.Play("Error");
                return;
            }
            AssignNewBind(joyEvent);
        }else if(@event is InputEventJoypadMotion joyMotion){
            if((joyMotion.Axis == JoyAxis.TriggerLeft || joyMotion.Axis == JoyAxis.TriggerRight) && joyMotion.AxisValue > 0.5f){
                AssignNewBind(joyMotion);
            }
        }
    }

    private void AssignNewBind(InputEvent newEvent){
        int actionIndex = Selection - 3;
        string targetAction = ControlProfileManager.REMAP_ACTIONS[actionIndex];
        string activeProfile = ControlProfileManager.Profiles[currentProfileIndex];

        ControlProfileManager.AddEventToProfile(activeProfile, targetAction, newEvent);
        
        isListening = false;
        remapCooldown = 0.2f;
        GetViewport().SetInputAsHandled();
        UpdateTexts();
        SFX.Play("Confirm");
    }

    private void UpdateTexts(){
        string activeProfile = ControlProfileManager.Profiles[currentProfileIndex];
        
        if(activeProfile == ControlProfileManager.DEFAULT_PROFILE){
            profileSelectorLabel.Text = $"< Profile: {activeProfile} (Read-Only) >";
        }else{
            profileSelectorLabel.Text = $"< Profile: {activeProfile} >";
        }
        
        Godot.Collections.Dictionary profileData = ControlProfileManager.GetProfileData(activeProfile);

        for(int i = 0; i < ControlProfileManager.REMAP_ACTIONS.Length; i++){
            string action = ControlProfileManager.REMAP_ACTIONS[i];
            string keyName = "Unassigned";
            List<string> bindNames = new List<string>();

            bool useDefault = activeProfile == ControlProfileManager.DEFAULT_PROFILE || !profileData.ContainsKey(action);

            if(!useDefault){
                Variant savedData = profileData[action];
                
                if(savedData.Obj is Godot.Collections.Array godotArray){
                    if(godotArray.Count > 0){
                        foreach(Variant v in godotArray){
                            if(v.Obj is string savedString){
                                InputEvent reconstructedEvent = ControlProfileManager.DeserializeEvent(savedString);
                                if(reconstructedEvent != null){
                                    bindNames.Add(FormatEventName(reconstructedEvent));
                                }
                            }
                        }
                        keyName = string.Join(", ", bindNames);
                    }
                }
            }
            
            if(useDefault){
                Godot.Collections.Array<InputEvent> defaultEvents = ControlProfileManager.GetHardcodedDefaults(action);
                if(defaultEvents.Count > 0){
                    foreach(InputEvent defEvent in defaultEvents){
                        bindNames.Add(FormatEventName(defEvent));
                    }
                    keyName = string.Join(", ", bindNames);
                }
            }

            actionLabels[i].Text = $"{action}: {keyName}";
        }

        bool vibrationEnabled = ControlProfileManager.GetVibration(activeProfile);
        vibrationLabel.Text = $"Vibration: {(vibrationEnabled ? "On" : "Off")}";
        
        createProfileLabel.Text = "+ Create New Profile";
    }

    private string FormatEventName(InputEvent @event){
        string rawName = @event.AsText();
        int bracketIndex = rawName.IndexOf(" (");
        if(bracketIndex > 0){
            rawName = rawName.Substring(0, bracketIndex);
        }
        return rawName;
    }
}