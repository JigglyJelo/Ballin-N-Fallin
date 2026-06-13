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

    private void SaveData(){
        Game.Save.SetValue("Accessibility","Dynamic Camera",DynamicCameraEnabled);
        Game.Save.SetValue("Accessibility","Always show names",AlwaysShowNames);
        Game.Save.Save(Game.SAVE_PATH);
    }

    public static void LoadData(){
        Game.Save.Load(Game.SAVE_PATH);
        DynamicCameraEnabled = (bool)Game.Save.GetValue("Accessibility", "Dynamic Camera", true);
        AlwaysShowNames = (bool)Game.Save.GetValue("Accessibility", "Always show names", false);
    }
}