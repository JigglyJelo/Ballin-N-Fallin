using Godot;
using System;

public partial class SoundMenu : VerticalMenu, ILeftRightSelections{
    private Label masterText, musicText, sfxText, soundtrackText;
    public bool InMusicSelection = false;

    public override void _Ready(){
        base._Ready();
        InMusicSelection = false;
        Selection = 1;
        totalSelections = 4;
        defaultFontSize = 1;
        LoadData();
        masterText = GetNode<Label>("Selections/Master Text");
        musicText = GetNode<Label>("Selections/Music Text");
        sfxText = GetNode<Label>("Selections/SFX Text");
        soundtrackText = GetNode<Label>("Selections/Soundtrack Text");
        UpdateSelectionVisual();
        UpdateTexts();
    }

    public override void _Process(double delta){
        if(!InMusicSelection){
			base._Process(delta);
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(Input.IsActionJustReleased("Y"+i)){
					SetToDefaultSettings();
				}
			}
		}
    }

    protected override void MenuChoose(int choice){
        SFX.Play("Confirm");
        switch(Selection){
            case 4:
                AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "Settings/SoundtrackDialog.tscn").Instantiate<SoundtrackDialog>());
                GD.Print("Opened");
                break;
        }
    }

    public override void MenuBack(){
        SFX.Play("Back");
        SaveData();
        MenuScene.LoadMenu("Settings/SettingsMenu");
    }

    public void MenuRight(){
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
        switch(Selection){
            case 1: 
                if(Game.MasterVolume < 100) Game.MasterVolume++;
                SetAudioVolume("Master",Game.MasterVolume);
                break;
            case 2: 
                if(Game.MusicVolume < 100) Game.MusicVolume++;
                SetAudioVolume("Music",Game.MusicVolume);
                break;
            case 3: 
                if(Game.SFXVolume < 100) Game.SFXVolume++;
                SetAudioVolume("SFX",Game.SFXVolume);
                break;
        }
        joystickTimer = 0;
        UpdateTexts();
    }

    public void MenuLeft(){
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
        switch(Selection){
            case 1: 
                if(Game.MasterVolume > 0) Game.MasterVolume--;
                SetAudioVolume("Master",Game.MasterVolume);
                break;
            case 2: 
                if(Game.MusicVolume > 0) Game.MusicVolume--;
                SetAudioVolume("Music",Game.MusicVolume);
                break;
            case 3: 
                if(Game.SFXVolume > 0) Game.SFXVolume--;
                SetAudioVolume("SFX",Game.SFXVolume);
                break;
        }
        joystickTimer = 0;
        UpdateTexts();
    }

    private void UpdateTexts(){
        masterText.Text = "Master Volume: " + Game.MasterVolume + "%";
        musicText.Text = "Music Volume: " + Game.MusicVolume + "%";
        sfxText.Text = "SFX Volume: " + Game.SFXVolume + "%";
        SaveData();
        LoadData();
    }

	private void SetToDefaultSettings(){
		Game.MasterVolume = 50;
		Game.MusicVolume = 50;
		Game.SFXVolume = 50;
		Game.CustomSoundtrack = "";
		UpdateTexts();
	}

    private void SaveData(){
        Game.SettingsSave.SetValue("Sound","Master Volume",Game.MasterVolume);
        Game.SettingsSave.SetValue("Sound","Music Volume",Game.MusicVolume);
        Game.SettingsSave.SetValue("Sound","SFX Volume",Game.SFXVolume);
        Game.SettingsSave.SetValue("Sound","Custom Soundtrack",Game.CustomSoundtrack);
        Game.SettingsSave.Save(Game.SETTINGS_PATH);
    }

    public static void LoadData(){
        Game.SettingsSave.Load(Game.SETTINGS_PATH);
        //Volume
        Game.MasterVolume = (byte)Game.SettingsSave.GetValue("Sound", "Master Volume", 50);
        Game.MusicVolume = (byte)Game.SettingsSave.GetValue("Sound", "Music Volume", 50);
        Game.SFXVolume = (byte)Game.SettingsSave.GetValue("Sound", "SFX Volume", 50);
        SetAudioVolume("Master",Game.MasterVolume);
        SetAudioVolume("Music",Game.MusicVolume);
        SetAudioVolume("SFX",Game.SFXVolume);
        Game.CustomSoundtrack = (string)Game.SettingsSave.GetValue("Sound", "Custom Soundtrack", "");
    }

    private static void SetAudioVolume(string bus, byte volume){
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(bus),(float)(20 * Math.Log10(volume / 100.0)));
    }
}