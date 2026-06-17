using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>The Master/Main class that acts as the root node</summary>
public partial class Game : Node{
    public static Game GameNode;
    public static SceneType CurrentScene;
    public static Camera2D Camera;
    public static ConfigFile Save = new ConfigFile();
    public static bool FirstBoot = true;
	public const int MAX_PLAYERS = 8;
    public const string SAVE_PATH = "user://Save.cfg";
    public const string LEVELS_PATH = "res://Levels/";
    public const int BASE_RES = 2160; //Base/Default Resolution of Ballin N Fallin 4k 2160p
    public static int TotalPlayers = 1;
    public static MouseModeEnum MouseMode = MouseModeEnum.Off;
    public static string CustomSoundtrack = "";
    public static Random Random = new Random();
    //Stores all the Players when in-game for easy access
    public static Player[] Players;
    public static List<PlayerData> PlayerDatas = new List<PlayerData>();
    public static List<PlayerData> SpectatorDatas = new List<PlayerData>();
    public static List<PlayerData> DisconnectedDatas = new List<PlayerData>();
    //Saves each Player's Controller Id 1-8
    public readonly static Dictionary<string, Color> Colors = new Dictionary<string, Color>{
    	{"Orange", Color.Color8(255,74,0)},
    	{"Lime", Color.Color8(0,255,59)},
    	{"Cyan", Color.Color8(0,166,255)},
    	{"Pink", Color.Color8(255,101,219)},
    	{"Red", Color.Color8(210,0,0)},
    	{"Green", Color.Color8(0,128,0)},
    	{"Blue", Color.Color8(0,30,255)},
    	{"Purple", Color.Color8(169,0,199)},
    	{"Yellow", Color.Color8(252,255,0)},
    	{"White", Color.Color8(245,245,245)},
    	{"Gray", Color.Color8(78,78,78)},
    	{"Brown", Color.Color8(134,33,0)}
	};
    public readonly static Color CLEAR = new Color(1,1,1,0);
    public readonly static Color ZEROES = new Color(0,0,0,0);
    public static Vector2 ContentScaleVector2;
    public static Vector2 ResolutionScaleVector2;
    public static List<Team> Teams = new List<Team>();
    public struct Team{
        string TeamName;
        Color TeamColor;
        List<int> PlayerIds = new List<int>();
        public Team(string teamName, Color teamColor){
            TeamName = teamName;
            TeamColor = teamColor;
        }
    }
    public static Color[] TeamColors = new Color[]{
        new Color(1,0,0),//Red
        new Color(0,0,1),//Blue
        new Color(1,1,0),//Yellow
        new Color(0,1,0),//Green
    };
    //Variables for gameplay
    public static Mode.GameMode CurrentMode = Mode.GameMode.None;
    public static string CurrentLevelName = "";
    public static string CurrentFolderPath = "";
    public static bool Paused = false;
    public static PackedScene CurrentLevel;
    public static string[] Events = {
        "Super","Epic", //1.5x Speed, 0.75x Speed
        "Impostor Syndrome","Holiday", //All players are same color, Lots of items
        "Reverse", "Speed", //Race backwards, First in Golf Hole wins
        "Bonus", "Lost",  //Double points, No arrow
        "Invisible", "Pandemic" //Inivisible Players, Colliding players get stunned
    };
    //Settings
    public static StompSettingEnum StompSetting = StompSettingEnum.On;
    public static byte MasterVolume, MusicVolume, SFXVolume;
    public static int Resolution = 2160;
    public static bool AutoStartNewTour = false;
    //Advanced Tour Settings
    public static bool WinnerTakesAll = false;
    public static bool RandomItems = false;
    public static float ItemFrequency = 50;
    public static bool IsDedicatedServer = false;

    public override void _Ready(){
        DisableProcesses(this);
        GD.Print("Ballin N Fallin V" + (string)ProjectSettings.GetSetting("application/config/version")); //+ " UTC: " + DateTime.UtcNow.ToShortDateString() + " " +  DateTime.UtcNow.ToShortTimeString()
        GameNode = this;
        Camera = GetNode<Camera2D>("Camera");
        Multiplayer.PeerDisconnected += Online.PlayerDisconnected;
        if(OS.GetCmdlineUserArgs().Contains("--server")){
            IsDedicatedServer = true;
            MenuScene.MenuToLoad = "OnlineMenu";
            GD.Print("Ballin N Fallin Server");
        }
        SceneTransitioner.SwitchToScene(SceneType.Menu);
    }

    public enum SceneType{
        Menu,Game,ScoreScreen
    }

    public enum StompSettingEnum{
        Off,On,TeamAttack
    }
    public static string StompEnumToString(StompSettingEnum stompSetting){
		switch(stompSetting){
            case StompSettingEnum.Off: return "Off";
            case StompSettingEnum.On: return "On";
            case StompSettingEnum.TeamAttack: return "Team Attack";
            default: return "Undefined Stomp Setting";
        }
	}
    public enum MouseModeEnum{
        Off,Cursor,Direction
    }
    //Methods
    public static bool UsingMouse(){
        return MouseMode != MouseModeEnum.Off;
    }

    public static void SetLevel(Mode.GameMode mode, string levelName,string folderPath){
        //Used to save to Rpc to clients
        if(Online.IsHost()){
            CurrentLevelName = levelName;
            CurrentFolderPath = folderPath;
        }

        string level = levelName;
        GD.Print(levelName);
        if(levelName.Contains(".remap")) level = levelName.Replace(".remap",""); //Needed for exported version of game
        CurrentLevel = GD.Load<PackedScene>(LEVELS_PATH + Mode.EnumToString(mode) + " Levels/" + folderPath + level);
    }
    public static void SetLevel(Mode.GameMode mode, string levelName){
        SetLevel(mode,levelName,"");
    }
    public static void SetLevel(PackedScene level){
        CurrentLevel = level;
    }

    public static void GetRandomLevel(Mode.GameMode mode){
        string[] levelNames;
        string[] folders = DirAccess.GetDirectoriesAt(LEVELS_PATH + Mode.EnumToString(mode) + " Levels/");
        string[] foldersWithRoot = new string[folders.Length+1];
        foldersWithRoot[0] = "";
        Array.Copy(folders,0,foldersWithRoot,1,folders.Length);
        List<string> nonEmptyFolders = new List<string>();
        foreach(string folder in foldersWithRoot){
            if(DirAccess.GetFilesAt(LEVELS_PATH + Mode.EnumToString(mode) + " Levels/"+ folder).Count() > 0){
                nonEmptyFolders.Add(folder);
            }
        }
        string randomFolder = nonEmptyFolders[Game.Random.Next(0,nonEmptyFolders.Count)];//foldersWithRoot[Game.Random.Next(0,foldersWithRoot.Length)];
        levelNames = DirAccess.GetFilesAt(LEVELS_PATH + Mode.EnumToString(mode) + " Levels/" + randomFolder);
        string levelName = levelNames[Random.Next(0,levelNames.Length)];
        string newLevelName = levelName;
        if(levelName.Contains(".remap")) newLevelName = levelName.Replace(".remap",""); //Needed for exported version of game
        SetLevel(mode,newLevelName,string.IsNullOrEmpty(randomFolder) ? "" : randomFolder+"/");
    }

    public static void ClearFontCache(){
        Font font = ResourceLoader.Load<FontFile>("res://Assets/Font/din1451alt.ttf");
        FontFile fontFile = (font as FontFile);
		fontFile.ClearCache();
		fontFile.LoadDynamicFont("res://Assets/Font/din1451alt.ttf");
        fontFile.Antialiasing = TextServer.FontAntialiasing.Gray;
        fontFile.AllowSystemFallback = true;
        //fontFile.DisableEmbeddedBitmaps = true;
        fontFile.GenerateMipmaps = true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SyncOnlinePlayerInfos(string[] usernames, byte[] inputIds, int[] uuids){
        PlayerDatas.Clear();
        for(int i = 0 ; i < usernames.Length; i++){
            PlayerDatas.Add(new PlayerData(usernames[i],(PlayerData.PlayerInputDevice)inputIds[i],uuids[i]));
        }
    }

    public static void TellClientsWhatToDoAboutDisconnectedPlayer(bool removePlayer, int uuid){
        GameNode.Rpc(nameof(GameNode.TellClientsWhatToDoAboutDisconnectedPlayerRpc), removePlayer, uuid);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void TellClientsWhatToDoAboutDisconnectedPlayerRpc(bool removePlayer, int uuid){
		PlayerData disconnectedPlayerInfo = null;
		int disconnectedPlayerId = -1;
		for(int i = 0; i < PlayerDatas.Count; i++){
			if(PlayerDatas[i].UUID == uuid){
				disconnectedPlayerInfo = PlayerDatas[i];
				disconnectedPlayerId = i+1;
				break;
			}
		}

		if(disconnectedPlayerInfo == null){
            int[] UUIDs = new int[PlayerDatas.Count];
            for(int i = 0; i < PlayerDatas.Count; i++) UUIDs[i] = PlayerDatas[i].UUID;
			GD.PrintErr("Could not find requested player to delete: " + uuid+ "\n" + string.Join(",",UUIDs));
		}else{
			if(removePlayer){
				DisconnectedDatas.Add((PlayerData)disconnectedPlayerInfo);
                Online.RemoveDisconnectedPlayerInfos();
                GD.Print("REMOVED "+ uuid);
			}else{
				Player player = Players[disconnectedPlayerId-1];
                Mode.ModeNode.PlayerDisconnected(player);
				DisconnectedDatas.Add((PlayerData)disconnectedPlayerInfo);
			}
		}
	}

    public static void SetResolution(){
        Window window = GameNode.GetTree().Root;
        if(Resolution >= DisplayServer.WindowGetSize().Y){
			window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            if(Resolution >= BASE_RES){ //Maybe change to just > after fixing texts?
                window.ContentScaleSize = DisplayServer.WindowGetSize();
                //window.ContentScaleSize = new Vector2I((int)Math.Ceiling((16f/9f) * Game.Resolution),Game.Resolution);
            }else{
                window.ContentScaleSize = new Vector2I((int)Math.Ceiling((16f/9f) * Resolution),Resolution);
            }
		}else{
			window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
			window.ContentScaleSize = new Vector2I((int)Math.Ceiling((16f/9f) * Resolution),Resolution);
		}
		MenuScene.SetCamera();
        UpdateContentScaleVector();
        UpdateResolutionScaleVector();
    }

    public static float GetResolutionScale(){
        return Resolution/2160f;
    }
    public static void UpdateResolutionScaleVector(){
        float scale = GetResolutionScale();
        if(ResolutionScaleVector2.X != scale && ResolutionScaleVector2.Y != scale){
            ResolutionScaleVector2 = new Vector2(scale,scale);
        }
    }
    public static float GetContentScale(){
        return GameNode.GetTree().Root.ContentScaleSize.Y / 2160f;
    }
    public static void UpdateContentScaleVector(){
        float scale = GetContentScale();
        ContentScaleVector2 = new Vector2(scale,scale);
    }
    public static void UpdateWindowSize(){
		if(Resolution > DisplayServer.ScreenGetSize().Y){
			int defaultResolution = VideoMenu.RESOLUTIONS[VideoMenu.GetDefaultResolution()-1];
			DisplayServer.WindowSetSize(new Vector2I((int)Math.Ceiling((16f/9f) * defaultResolution),defaultResolution));
		}else{
			DisplayServer.WindowSetSize(new Vector2I((int)Math.Ceiling((16f/9f) * Resolution),Resolution));
		}
	}

    public static Color GetPlayerColor(int id){
        return PlayerDatas[id-1].PlayerColor;
    }

    public static string GetUsername(int id){
        if(Online.IsOnline){
            if(PlayerDatas[id-1].UUID == Game.GameNode.Multiplayer.GetUniqueId()) return Online.Username;
            string username = PlayerDatas[id - 1].Username;
            if(username.Length <= Online.USERNAME_LENGTH) return username;
			return username.Substring(0,Online.USERNAME_LENGTH)+"...";
        }else{
            if(PlayerDatas[id-1].ControlProfileName == ControlProfileManager.DEFAULT_PROFILE)
                return (Game.CurrentScene == SceneType.Game ? "P" : "Player ") + id;
            else
                return PlayerDatas[id-1].ControlProfileName;
        }
    }
    
    public static void DisableProcesses(Node node){
        node.SetProcess(false);
        node.SetPhysicsProcess(false);
        node.SetProcessInput(false);
        node.SetProcessShortcutInput(false);
        node.SetProcessUnhandledInput(false);
        node.SetProcessUnhandledKeyInput(false);
    }
}