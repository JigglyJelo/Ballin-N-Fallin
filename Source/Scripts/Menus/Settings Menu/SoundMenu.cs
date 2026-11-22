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
                GD.Print("Opened soundtrack dialog");
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

    private async void UpdateTexts(){
        masterText.Text = "Master Volume: " + Game.MasterVolume + "%";
        musicText.Text = "Music Volume: " + Game.MusicVolume + "%";
        sfxText.Text = "SFX Volume: " + Game.SFXVolume + "%";
        SaveData();
        LoadData();
		//Manually set back scales to what they should be since updating text sometimes resets it to 1.0
		if(Selections != null){
			Vector2 ogScale = new Vector2(VerticalMenu.SELECTION_SCALE,VerticalMenu.SELECTION_SCALE);
			Label selectedLabel = Selections[Selection-1] as Label;
			selectedLabel.Scale = ogScale;
			await ToSignal(GetTree().CreateTimer(0.001,true,false,true), "timeout");
			selectedLabel.Scale = ogScale;
		}
    }

	private void SetToDefaultSettings(){
		Game.MasterVolume = 50;
		Game.MusicVolume = 50;
		Game.SFXVolume = 50;
		Game.CustomSoundtrack = "";
		UpdateTexts();
	}

    private void SaveData(){
        SaveManager.SettingsConfig.SetValue(SaveManager.AUDIO_SECTION,"Master Volume",Game.MasterVolume);
        SaveManager.SettingsConfig.SetValue(SaveManager.AUDIO_SECTION,"Music Volume",Game.MusicVolume);
        SaveManager.SettingsConfig.SetValue(SaveManager.AUDIO_SECTION,"SFX Volume",Game.SFXVolume);
        SaveManager.SettingsConfig.SetValue(SaveManager.AUDIO_SECTION,"Custom Soundtrack",Game.CustomSoundtrack);
        SaveManager.SettingsConfig.Save(SaveManager.SETTINGS_PATH);
    }

    public static void LoadData(){
        SaveManager.SettingsConfig.Load(SaveManager.SETTINGS_PATH);
        //Volume
        Game.MasterVolume = (byte)SaveManager.SettingsConfig.GetValue(SaveManager.AUDIO_SECTION, "Master Volume", 50);
        Game.MusicVolume = (byte)SaveManager.SettingsConfig.GetValue(SaveManager.AUDIO_SECTION, "Music Volume", 50);
        Game.SFXVolume = (byte)SaveManager.SettingsConfig.GetValue(SaveManager.AUDIO_SECTION, "SFX Volume", 50);
        SetAudioVolume("Master",Game.MasterVolume);
        SetAudioVolume("Music",Game.MusicVolume);
        SetAudioVolume("SFX",Game.SFXVolume);
        Game.CustomSoundtrack = (string)SaveManager.SettingsConfig.GetValue(SaveManager.AUDIO_SECTION, "Custom Soundtrack", "");
    }

    private static void SetAudioVolume(string bus, byte volume){
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(bus),(float)(20 * Math.Log10(volume / 100.0)));
    }
}