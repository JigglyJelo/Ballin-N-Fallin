using Godot;
using System.Collections.Generic;

public static class ControlProfileManager{
    public const string DEFAULT_PROFILE = "Default";
    public static readonly string[] REMAP_ACTIONS = {"Charge N Launch", "Slam", "Item", "Y"};
    public static List<string> Profiles = new List<string>{DEFAULT_PROFILE, "Fortnite"};

    //Custom input save serialization
    public static string SerializeEvent(InputEvent @event){
        if(@event is InputEventJoypadButton btn){
            return $"JoyBtn:{(int)btn.ButtonIndex}";
        }else if(@event is InputEventJoypadMotion motion){
            return $"JoyAxis:{(int)motion.Axis}:{(motion.AxisValue > 0 ? "1" : "-1")}";
        }
        return "Unknown";
    }

    public static InputEvent DeserializeEvent(string data){
        string[] parts = data.Split(':');
        if(parts.Length < 2) return null;

        if(parts[0] == "JoyBtn"){
            InputEventJoypadButton btn = new InputEventJoypadButton();
            btn.ButtonIndex = (JoyButton)int.Parse(parts[1]);
            return btn;
        }else if(parts[0] == "JoyAxis" && parts.Length == 3){
            InputEventJoypadMotion motion = new InputEventJoypadMotion();
            motion.Axis = (JoyAxis)int.Parse(parts[1]);
            motion.AxisValue = float.Parse(parts[2]);
            return motion;
        }
        return null;
    }

    public static void LoadProfiles(){
        Profiles.Clear();
        Profiles.Add(DEFAULT_PROFILE);

        if(Game.Save.HasSection("Controls")){
            string[] savedProfiles = Game.Save.GetSectionKeys("Controls");
            foreach(string profile in savedProfiles){
                if(profile != DEFAULT_PROFILE && !Profiles.Contains(profile)){
                    Profiles.Add(profile);
                }
            }
        }
    }

    public static void CreateNewProfile(string profileName){
        if(!Profiles.Contains(profileName)){
            Profiles.Add(profileName);
            SaveProfileData(profileName, new Godot.Collections.Dictionary()); 
        }
    }

    public static Godot.Collections.Dictionary GetProfileData(string profileName){
        Variant defaultDict = new Godot.Collections.Dictionary();
        Variant data = Game.Save.GetValue("Controls", profileName, defaultDict);
        if(data.Obj is Godot.Collections.Dictionary dict) return dict;
        return new Godot.Collections.Dictionary();
    }

    private static void SaveProfileData(string profileName, Godot.Collections.Dictionary data){
        Game.Save.SetValue("Controls", profileName, data);
        Game.Save.Save(Game.SAVE_PATH);
    }

    public static Godot.Collections.Array<InputEvent> GetHardcodedDefaults(string action){
        Godot.Collections.Array<InputEvent> events = new Godot.Collections.Array<InputEvent>();
        switch(action){
            case "Charge N Launch":
                InputEventJoypadButton aBtn = new InputEventJoypadButton();
                aBtn.ButtonIndex = JoyButton.A; 
                events.Add(aBtn);
                break;
            case "Slam":
                InputEventJoypadButton xBtn = new InputEventJoypadButton();
                xBtn.ButtonIndex = JoyButton.X; 
                events.Add(xBtn);
                break;
            case "Item":
                InputEventJoypadButton lbBtn = new InputEventJoypadButton();
                lbBtn.ButtonIndex = JoyButton.LeftShoulder; 
                events.Add(lbBtn);
                
                InputEventJoypadButton rbBtn = new InputEventJoypadButton();
                rbBtn.ButtonIndex = JoyButton.RightShoulder; 
                events.Add(rbBtn);
                
                InputEventJoypadMotion ltMotion = new InputEventJoypadMotion();
                ltMotion.Axis = JoyAxis.TriggerLeft; 
                ltMotion.AxisValue = 1.0f;
                events.Add(ltMotion);
                
                InputEventJoypadMotion rtMotion = new InputEventJoypadMotion();
                rtMotion.Axis = JoyAxis.TriggerRight; 
                rtMotion.AxisValue = 1.0f;
                events.Add(rtMotion);
                break;
            case "Y":
                InputEventJoypadButton yBtn = new InputEventJoypadButton();
                yBtn.ButtonIndex = JoyButton.Y; 
                events.Add(yBtn);
                break;
        }
        return events;
    }

    public static void ApplyProfileToDevice(string profileName, int inputId){
        if(inputId < 0 || inputId >= 8) return;

        Godot.Collections.Dictionary profileData = GetProfileData(profileName);

        foreach(string action in REMAP_ACTIONS){
            string fullActionName = action + inputId; 

            if(!InputMap.HasAction(fullActionName)){
                InputMap.AddAction(fullActionName);
            }

            InputMap.ActionEraseEvents(fullActionName);

            bool useDefault = profileName == DEFAULT_PROFILE || !profileData.ContainsKey(action);

            if(!useDefault){
                Variant savedData = profileData[action];
                
                if(savedData.Obj is Godot.Collections.Array godotArray){
                    foreach(Variant v in godotArray){
                        if(v.Obj is string savedString){
                            InputEvent reconstructedEvent = DeserializeEvent(savedString);
                            if(reconstructedEvent != null){
                                reconstructedEvent.Device = inputId;
                                InputMap.ActionAddEvent(fullActionName, reconstructedEvent);
                            }
                        }
                    }
                }
            }
            
            if(useDefault){
                Godot.Collections.Array<InputEvent> defaultEvents = GetHardcodedDefaults(action);
                foreach(InputEvent defEvent in defaultEvents){
                    InputEvent clonedDefault = (InputEvent)defEvent.Duplicate();
                    clonedDefault.Device = inputId;
                    InputMap.ActionAddEvent(fullActionName, clonedDefault);
                }
            }
        }
    }

    public static void AddEventToProfile(string profileName, string action, InputEvent @event){
        Godot.Collections.Dictionary profileData = GetProfileData(profileName);
        Godot.Collections.Array eventsArray = new Godot.Collections.Array();

        if(profileData.ContainsKey(action)){
            Variant savedData = profileData[action];
            if(savedData.Obj is Godot.Collections.Array godotArray){
                eventsArray = godotArray;
            }
        }

        string serializedEvent = SerializeEvent(@event);
        if(serializedEvent == "Unknown") return; // Safety check
        
        bool alreadyExists = false;
        foreach(Variant v in eventsArray){
            if(v.Obj is string existingString && existingString == serializedEvent){
                alreadyExists = true;
                break;
            }
        }

        if(!alreadyExists){
            eventsArray.Add(serializedEvent);
            profileData[action] = eventsArray;
            SaveProfileData(profileName, profileData);
        }
    }

    public static void ClearEventsFromProfile(string profileName, string action){
        Godot.Collections.Dictionary profileData = GetProfileData(profileName);
        profileData[action] = new Godot.Collections.Array();
        SaveProfileData(profileName, profileData);
    }

    public static void RestoreFactoryDefault(string profileName, string action){
        Godot.Collections.Dictionary profileData = GetProfileData(profileName);
        if(profileData.ContainsKey(action)){
            profileData.Remove(action); 
            SaveProfileData(profileName, profileData);
        }
    }

    public static bool GetVibration(string profileName){
        if(profileName == DEFAULT_PROFILE) return true; 
        Godot.Collections.Dictionary profileData = GetProfileData(profileName);
        if(profileData.ContainsKey("VibrationEnabled")){
            return (bool)profileData["VibrationEnabled"];
        }
        return true; 
    }

    public static void SetVibration(string profileName, bool enabled){
        if(profileName == DEFAULT_PROFILE) return; 
        
        Godot.Collections.Dictionary profileData = GetProfileData(profileName);
        profileData["VibrationEnabled"] = enabled;
        SaveProfileData(profileName, profileData);
    }
}