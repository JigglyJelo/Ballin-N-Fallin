public partial class SettingsMenu : VerticalMenu{

    public override void _Ready(){
        base._Ready();
        Selection = 1;
        totalSelections = 4;
        defaultFontSize = 1;
        LoadData();
        UpdateSelectionVisual();
    }

    protected override void MenuChoose(int choice){
        SFX.Play("Confirm");
        switch(Selection){
            case 1: MenuScene.LoadMenu("Settings/VideoMenu"); break;
            case 2: MenuScene.LoadMenu("Settings/SoundMenu"); break;
            case 3: MenuScene.LoadMenu("Settings/ProfileMenu"); break;
            case 4: MenuScene.LoadMenu("Settings/AccessibilityMenu"); break;
        }
    }

    public override void MenuBack(){
        SFX.Play("Back");
        MenuScene.LoadMenu("MainMenu");
    }

    public static void LoadData(){
        VideoMenu.LoadData();
        SoundMenu.LoadData();
        ControlProfileManager.LoadProfiles();
        AccessibilityMenu.LoadData();
    }
}