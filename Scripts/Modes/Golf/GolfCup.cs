using Godot;
using System;
using System.Collections.Generic;

public partial class GolfCup : Node{
	public static bool IsCup = false;
	public const float MAX_JUICE = 6;
	public static string CupName;
	public static List<string> HoleNames = new List<string>();
	public static List<sbyte> HolePars = new List<sbyte>();
	public static List<sbyte> Scores = new List<sbyte>();
	public static short TotalCupPar;
	public static Vector2 MulliganPosition;
	private static float juice;
	private static List<string> folders;
	public static float Juice{
		get{return juice;}
		set{
			if(IsCup){
				if(value > MAX_JUICE) juice = MAX_JUICE;
				else if(value > 0) juice = value;
				else juice = 0;
			}else juice = value;
		}
	}

	public static void PrepareCup(List<string> folders){
		CupName = folders[folders.Count - 1].Replace("/","");
		HoleNames = new List<string>();
		HolePars = new List<sbyte>(); //Add when level loads
		Scores = new List<sbyte>();
		TotalCupPar = 0;
		GolfCup.folders = folders;
		foreach(string file in DirAccess.GetFilesAt(Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + string.Join("",folders))){
			HoleNames.Add(file);
		}
		Juice = 2;
		Game.SetLevel(Mode.GameMode.Golf,HoleNames[0],string.Join("",folders));
	}
}