using Godot;
using System;
using System.Collections.Generic;

public partial class CoinSpawner : Sprite2D{
	[Export]
	public Godot.Collections.Array<CoinPattern> CoinPatterns;

	private const string PATH = "res://Source/Scenes/Object Scenes/Mode Stuff/Ballin to the Bank/Coin Patterns";
	public readonly static PackedScene[] COIN_PATTERNS = LoadCoinPatterns();
	private static PackedScene[] LoadCoinPatterns(){
		List<PackedScene> patterns = new List<PackedScene>();
		foreach(string file in DirAccess.GetFilesAt(PATH)){
			patterns.Add(GD.Load<PackedScene>(PATH+"/"+file.Replace(".remap",""))); //Remap for exported build
		}
		return patterns.ToArray();
	}
	private readonly static string[] COIN_PATTERN_NAMES = LoadCoinPatternNames();
	private static string[] LoadCoinPatternNames(){
		List<string> names = new List<string>();
		foreach(string file in DirAccess.GetFilesAt(PATH)){
			names.Add(file.Replace(".remap",""));
		}
		return names.ToArray();
	}

	public enum CoinPattern{
		FivePlus,FourSquare,SevenHexagon,Single,SixHorizontalRectangle,SixVerticalRectangle
	}
	public static string EnumToString(CoinPattern coinPattern){
		switch(coinPattern){
			case CoinPattern.Single: return "Single";
			case CoinPattern.FourSquare: return "FourSquare";
			case CoinPattern.FivePlus: return "FivePlus";
			case CoinPattern.SevenHexagon: return "SevenHexagon";
			case CoinPattern.SixHorizontalRectangle: return "SixHorizontalRectangle";
			case CoinPattern.SixVerticalRectangle: return "SixVerticalRectangle";
		}
		return "";
	}

	public static int EnumToIndex(CoinPattern coinPattern){
		return Array.BinarySearch(COIN_PATTERN_NAMES,EnumToString(coinPattern)+".tscn");
	}
}