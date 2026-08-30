using Godot;
using System;

public partial class AccessibilityMenu : VerticalMenu{
    public static bool DynamicCameraEnabled, AlwaysShowNames;
    private Label cameraText, nameText;

    public override void _Ready(){
        base._Ready();
        Selection = 1;
        totalSelections = 2;
        defaultFontSize = 1;
        LoadData();
        cameraText = GetNode<Label>("Selections/Camera Text");
        nameText = GetNode<Label>("Selections/Name Text");
        UpdateSelectionVisual();
        UpdateTexts();
    }

	public override void _Process(double delta){
		base._Process(delta);
		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Y"+i)){
				SetToDefaultSettings();
			}
		}
	}

    protected override void MenuChoose(int choice){
        switch(choice){
            case 1: DynamicCameraEnabled = !DynamicCameraEnabled; break;
            case 2: AlwaysShowNames = !AlwaysShowNames; break;
        }
        UpdateTexts();
    }

    public override void MenuBack(){
        SFX.Play("Back");
        SaveData();
        MenuScene.LoadMenu("Settings/SettingsMenu");
    }

    private void UpdateTexts(){
        cameraText.Text = "Dynamic Camera: " + (DynamicCameraEnabled ? "On" : "Off");
        nameText.Text = "Display Names: " + (AlwaysShowNames ? "Always" : "At start");
        SaveData();
        LoadData();
    }

	private void SetToDefaultSettings(){
		DynamicCameraEnabled = true;
		AlwaysShowNames = false;
		UpdateTexts();
	}

    private void SaveData(){
        Game.SettingsSave.SetValue("Accessibility","Dynamic Camera",DynamicCameraEnabled);
        Game.SettingsSave.SetValue("Accessibility","Always show names",AlwaysShowNames);
        Game.SettingsSave.Save(Game.SETTINGS_PATH);
    }

    public static void LoadData(){
        Game.SettingsSave.Load(Game.SETTINGS_PATH);
        DynamicCameraEnabled = (bool)Game.SettingsSave.GetValue("Accessibility", "Dynamic Camera", true);
        AlwaysShowNames = (bool)Game.SettingsSave.GetValue("Accessibility", "Always show names", false);
    }
}