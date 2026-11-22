using Godot;

public static class SaveManager{
    public static ConfigFile SettingsConfig = new ConfigFile();
    public const string SETTINGS_PATH = "user://Settings.cfg";
    public const string VIDEO_SECTION = "Video";
    public const string AUDIO_SECTION = "Sound";
    public const string ONLINE_SECTION = "Online";
    public const string ACCESSIBILITY_SECTION = "Accessibility";
    public static ConfigFile ControlsConfig = new ConfigFile();
    public const string CONTROLS_PATH = "user://Controls.cfg";
    public const string CONTROLS_SECTION = "Controls";
    public const string SAVE_PATH = "user://Records.sav";

    public static float GetSavedLevelRecord(Mode.GameMode modeEnum, string levelName){
        if(FileAccess.FileExists(SAVE_PATH)){
            using FileAccess file = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
            if(file != null){
                Godot.Collections.Dictionary timesDict = (Godot.Collections.Dictionary)file.GetVar();
                string levelKey = Mode.EnumToString(modeEnum) + levelName;
                if(timesDict.ContainsKey(levelKey)){
                    return (float)timesDict[levelKey];
                }
            }
        }
        
        return float.NaN;
    }

    public static void SaveLevelRecord(Mode.GameMode modeEnum, string levelName, float record){
        Godot.Collections.Dictionary timesDict = new Godot.Collections.Dictionary();

        if(FileAccess.FileExists(SAVE_PATH)){
            using FileAccess loadFile = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Read);
            if(loadFile != null){
                timesDict = (Godot.Collections.Dictionary)loadFile.GetVar();
            }
        }
        timesDict[Mode.EnumToString(modeEnum) + levelName] = record;

        using FileAccess saveFile = FileAccess.Open(SAVE_PATH, FileAccess.ModeFlags.Write);
        if(saveFile != null){
            saveFile.StoreVar(timesDict);
        }
    }

    public static void LoadSaveData(){
        SettingsConfig.Load(SETTINGS_PATH);
        VideoMenu.LoadData();
        SoundMenu.LoadData();
        AccessibilityMenu.LoadData();
        SettingsOnlineMenu.LoadData();
        ControlsConfig.Load(CONTROLS_PATH);
        ControlProfileManager.LoadProfiles();
    }
}