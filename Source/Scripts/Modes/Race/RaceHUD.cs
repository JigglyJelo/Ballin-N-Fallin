using Godot;
using System;

public partial class RaceHUD : Node{
	public static string LevelName = "";
	private Label raceTimerText,lapText;
	private float[] medals;
	private float personalBest;
	public override void _Ready(){
		personalBest = Game.GetSavedLevelRecord(Mode.GameMode.Race,LevelName);
		medals = (float[])Mode.ModeNode.GetNode<Level>("Level").GetMeta("Medals",new float[]{0,0,0,0});
		GetNode<CanvasLayer>("CanvasLayer").Scale = Game.ContentScaleVector2;
		raceTimerText = GetNode<Label>("CanvasLayer/TimerText");
		lapText = GetNode<Label>("CanvasLayer/LapText");
		lapText.SelfModulate = Game.PlayerDatas[0].PlayerColor;
		GD.Print(personalBest);
	}

	public override void _PhysicsProcess(double delta){
		if(!Mode.Finished) raceTimerText.Text = TimeSpan.FromSeconds(Race.RaceTimer).ToString("m':'ss':'fff");

		if(Race.RaceTimer < medals[3] && personalBest <= medals[2]) raceTimerText.SelfModulate = LevelMenu.DIAMOND_COLOR; //Diamond (Can only be seen if you earned Gold)
		else if(Race.RaceTimer < medals[2]) raceTimerText.SelfModulate = LevelMenu.GOLD_COLOR;
		else if(Race.RaceTimer < medals[1]) raceTimerText.SelfModulate = LevelMenu.SILVER_COLOR;
		else if(Race.RaceTimer < medals[0]) raceTimerText.SelfModulate = LevelMenu.BRONZE_COLOR;
		else raceTimerText.SelfModulate = Colors.White;

		if(Race.PlayerLaps.Length > 0 && !lapText.Text.Equals("Lap " + (Race.PlayerLaps[0] + 1) + "/" + Race.TotalLaps)){
			lapText.Text = "Lap " + (Race.PlayerLaps[0] + 1) + "/" + Race.TotalLaps;
		}
	}

	public override void _Process(double delta){
		if(Mode.Finished){
			SaveData();
			QueueFree();
		}
	}

	private void SaveData(){
		if(Race.RaceTimer < personalBest || float.IsNaN(personalBest)){
			Game.SaveLevelRecord(Mode.GameMode.Race,LevelName,Race.RaceTimer);
			GD.Print("Saved");
		}
	}
}