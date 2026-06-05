using System.Collections.Generic;

public class Tour{
	public static bool IsTour = true;
	public static TourSettings CurrentTour = new TourSettings();
	//Saves each Controller's Player Score
	public static int[] PlayerScores = new int[Game.MAX_PLAYERS];
	public static Dictionary<Mode.GameMode,bool> EnabledGameModes = new Dictionary<Mode.GameMode, bool>{
        {Mode.GameMode.Race,true}, {Mode.GameMode.Domination,true}, {Mode.GameMode.BallinToTheBank,true},{Mode.GameMode.Deathmatch,true},{Mode.GameMode.Survival,true},
        {Mode.GameMode.TargetTest,true},{Mode.GameMode.Miscellaneous,false},{Mode.GameMode.KingOfTheHill,true},{Mode.GameMode.CrownTheKing,true},{Mode.GameMode.HotPotato,true},
        {Mode.GameMode.Golf,true},{Mode.GameMode.Soccer,true},{Mode.GameMode.Volleyball,true},{Mode.GameMode.Payload,true},{Mode.GameMode.BombBall,true},
    };
	private static List<Mode.GameMode> modes;

	public static void GenerateModesList(){
        foreach(KeyValuePair<Mode.GameMode,bool> modeToggle in EnabledGameModes){
            if(modeToggle.Value) modes.Add(modeToggle.Key);
        }
    }

    public static void PrepareTour(){
        ResetPlayerScores();
        Game.CurrentMode = Mode.GameMode.None;
        modes = new List<Mode.GameMode>();
        GenerateModesList();
        Game.CurrentMode = ChooseNextMode();
        Game.GetRandomLevel(Game.CurrentMode);
    }

    public static Mode.GameMode ChooseNextMode(){
        Mode.GameMode nextMode;
        if(modes.Count != 0){
            nextMode = modes[Game.Random.Next(0, modes.Count)];
            modes.RemoveAt(modes.IndexOf(nextMode));
        }else{
            GenerateModesList();
            nextMode = modes[Game.Random.Next(0, modes.Count)];
        }
        return nextMode;
    }

	public static void GameFinishedPoints(byte[] points, byte[] positions){
        for(int i = 0; i < Game.TotalPlayers; i++){
            points[i] = GetIncreaseAmount(positions[i]);
        }
        IncreasePlayerScores(points);
    }

    public static byte GetIncreaseAmount(byte position){
        switch(Game.TotalPlayers){
            case 1:
                if(position == 1) return 10;
                return 0;
            case 2:
                if(position == 1) return 10;
                return 0;
            case 3:
                switch(position){
                    case 1: return 10;
                    case 2: return 5;
                    case 3: return 0;
                }
                break;
            case 4:
                switch(position){
                    case 1: return 10;
                    case 2: return 6;
                    case 3: return 2;
                    case 4: return 0;
                }
                break;
            case 5:
                switch(position){
                    case 1: return 10;
                    case 2: return 6;
                    case 3: return 2;
                    case 4: return 1;
                    case 5: return 0;
                }
                break;
            case 6:
                switch(position){
                    case 1: return 10;
                    case 2: return 7;
                    case 3: return 5;
                    case 4: return 3;
                    case 5: return 1;
                    case 6: return 0;
                }
                break;
            case 7:
                switch(position){
                    case 1: return 10;
                    case 2: return 7;
                    case 3: return 5;
                    case 4: return 3;
                    case 5: return 2;
                    case 6: return 1;
                    case 7: return 0;
                }
                break;
            case 8:
                switch(position){
                    case 1: return 10;
                    case 2: return 7;
                    case 3: return 5;
                    case 4: return 4;
                    case 5: return 3;
                    case 6: return 2;
                    case 7: return 1;
                    case 8: return 0;
                }
                break;
        }
        return byte.MaxValue; //Should never occur
    }

    private static void IncreasePlayerScores(byte[] increases){
        for(int i = 0; i < Game.TotalPlayers; i++){
            //int index = Online.IsOnline ? i : Game.InputIds[i]-1;
            int index = i;
            PlayerScores[index] += increases[i];
        }
    }

    public static void ResetPlayerScores(){
        for(int i = 0; i < PlayerScores.Length; i++) PlayerScores[i] = 0;
    }

	public struct TourSettings{
		public int PointsToWin;
		public bool ItemsEnabled;
		public bool EventsEnabled;
		public Game.StompSettingEnum StompSetting;
		public Mode.GameMode[] EnabledModes;

		public TourSettings(){
			PointsToWin = 50;
			ItemsEnabled = true;
			EventsEnabled = true;
			StompSetting = Game.StompSettingEnum.On;
			EnabledModes = new Mode.GameMode[]{
				Mode.GameMode.Race ,Mode.GameMode.Golf,Mode.GameMode.KingOfTheHill,
				Mode.GameMode.Deathmatch,Mode.GameMode.Soccer,Mode.GameMode.CrownTheKing,
				Mode.GameMode.Survival,Mode.GameMode.Volleyball,Mode.GameMode.HotPotato,
                Mode.GameMode.BallinToTheBank
			};
		}
		
		public TourSettings(int pointsToWin, bool itemsEnabled, bool eventsEnabled, Game.StompSettingEnum stompSetting, Mode.GameMode[] enabledModes){
			PointsToWin = pointsToWin;
			ItemsEnabled = itemsEnabled;
			EventsEnabled = eventsEnabled;
			StompSetting = stompSetting;
			EnabledModes = enabledModes;
		}
	}
}