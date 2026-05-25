using Godot;
using System;

public partial class VideoMenu : VerticalMenu, ILeftRightSelections{
    private Label resolutionText, fpsText, vsyncText, fullscreenText;
    public static readonly short[] RESOLUTIONS = {360,486,720,1080,1440,2160,2880,4320};
    
    private static byte resolutionIndex = 0;
    private static int fpsCap = 0; // 0 is Unlimited, otherwise 60-1000
    private static bool vSyncEnabled = true;

    public override void _Ready(){
        base._Ready();
        Selection = 1;
        totalSelections = 4;
        defaultFontSize = 1;
        LoadData();
        resolutionText = GetNode<Label>("Selections/Resolution Text");
        fpsText = GetNode<Label>("Selections/FPS Text");
        vsyncText = GetNode<Label>("Selections/V-Sync Text");
        fullscreenText = GetNode<Label>("Selections/Fullscreen Text");
        UpdateSelectionVisual();
        UpdateTexts();
    }

    protected override void MenuChoose(int choice){}

    public override void MenuBack(){
        SFX.Play("Back");
        SaveData();
        MenuScene.LoadMenu("Settings/SettingsMenu");
        QueueFree();
    }

    public void MenuRight(){
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
        switch(Selection){
            case 1:
                if(resolutionIndex < RESOLUTIONS.Length - 1) resolutionIndex++;
                break;
            case 2: 
                if(fpsCap == 0) fpsCap = 60;
                else if(fpsCap < 1000) fpsCap++;
                break;
            case 3: 
                vSyncEnabled = !vSyncEnabled;
                break;
            case 4: 
                if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.ExclusiveFullscreen) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                else DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
                break;
        }
        joystickTimer = 0;
        UpdateTexts();
    }

    public void MenuLeft(){
        SFX.Play("Move",Game.Random.Next(80,110)/100f);
        switch(Selection){
            case 1:
                if(resolutionIndex > 0) resolutionIndex--;
                break;
            case 2: 
                if(fpsCap == 60) fpsCap = 0;
                else if(fpsCap > 60) fpsCap--;
                break;
            case 3: 
                vSyncEnabled = !vSyncEnabled;
                break;
            case 4: 
                if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.ExclusiveFullscreen) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                else DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
                break;
        }
        joystickTimer = 0;
        UpdateTexts();
    }

    private void UpdateTexts(){
        resolutionText.Text = "Resolution: " + Math.Ceiling(16f/9f * RESOLUTIONS[resolutionIndex]) + " x " + RESOLUTIONS[resolutionIndex];
        
        if(fpsCap == 0) fpsText.Text = "FPS Cap: Unlimited";
        else fpsText.Text = "FPS Cap: " + fpsCap;
        
        vsyncText.Text = vSyncEnabled ? "V-Sync: On" : "V-Sync: Off";
        fullscreenText.Text = (DisplayServer.WindowGetMode().Equals(DisplayServer.WindowMode.ExclusiveFullscreen)) ? "Fullscreen: On" : "Fullscreen: Off";
        SaveData();
        LoadData();
    }

    private void SaveData(){
        Game.Save.SetValue("Video","Resolution",resolutionIndex);
        Game.Save.SetValue("Video","Framerate Cap",fpsCap);
        Game.Save.SetValue("Video","VSync",vSyncEnabled);
        Game.Save.SetValue("Video","Fullscreen",(int)DisplayServer.WindowGetMode());
        Game.Save.Save(Game.SETTINGS_PATH);
    }

    public static void LoadData(){
        Game.Save.Load(Game.SETTINGS_PATH);
        //Resolution
        resolutionIndex = (byte)Game.Save.GetValue("Video","Resolution", GetDefaultResolution());
        int oldResolution = Game.Resolution;
        Game.Resolution = RESOLUTIONS[resolutionIndex];

        //Framerate
        fpsCap = (int)Game.Save.GetValue("Video","Framerate Cap", GetDefaultFPS());
        //Clamp loaded data just in case of file modification
        if (fpsCap < 60 && fpsCap != 0) fpsCap = 0;
        Engine.MaxFps = fpsCap;

        //VSync
        vSyncEnabled = (bool) Game.Save.GetValue("Video","VSync", true);
        DisplayServer.WindowSetVsyncMode(vSyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

        //Screen Mode
        if((int)Game.Save.GetValue("Video","Fullscreen", (int)DisplayServer.WindowMode.ExclusiveFullscreen) == (int)DisplayServer.WindowMode.ExclusiveFullscreen){
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
        }else if((int)Game.Save.GetValue("Video","Fullscreen",(int)DisplayServer.WindowMode.ExclusiveFullscreen) == (int)DisplayServer.WindowMode.Windowed){
            if(oldResolution != Game.Resolution) DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            Game.UpdateWindowSize();
        }
        Game.SetResolution();
    }

    public static int GetDefaultResolution(){
        int resolution = DisplayServer.ScreenGetSize().Y;
        int index = RESOLUTIONS.Length-1;
        for(int i = 0; i < RESOLUTIONS.Length; i++){
            if(resolution <= RESOLUTIONS[i]){
                index = i;
                break;
            }
        }
        GD.Print("Monitor Resolution: " + resolution + " Setting target resolution to " + RESOLUTIONS[index]);
        return index;
    }

    public static int GetDefaultFPS(){
        float refreshRate = DisplayServer.ScreenGetRefreshRate();
        int targetFps = 0; //Default to Unlimited

        if(refreshRate >= 60f){
            targetFps = (int)Math.Round(refreshRate); // Round so 143.99 snaps to 144
            if(targetFps > 1000) targetFps = 1000;
        }else if(refreshRate > 0f){
            targetFps = 60; //Fallback for 59Hz/50Hz screens
        }

        GD.Print("Monitor Refresh Rate: " + (refreshRate > 0 ? refreshRate : "UNABLE TO DETECT ") + " Setting FPS Cap to " + (targetFps == 0 ? "Unlimited" : targetFps.ToString()));
        return targetFps;
    }
}