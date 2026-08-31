using Godot;
using System;

public partial class SurvivalHUD : Node{
	public static string LevelName = "";
	private Label timerText;
	private float[] medals;
	private float personalBest;
	public override void _Ready(){
		personalBest = Game.GetSavedLevelRecord(Mode.GameMode.Survival,LevelName);
		medals = (float[])Mode.ModeNode.GetNode<Level>("Level").GetMeta("Medals",new float[]{0,0,0,0});
		GetNode<CanvasLayer>("CanvasLayer").Scale =  Game.ContentScaleVector2;
		timerText = GetNode<Label>("CanvasLayer/TimerText");
		GD.Print(personalBest);
	}

	public override void _PhysicsProcess(double delta){
		if(!Mode.Finished) timerText.Text = TimeSpan.FromSeconds(Survival.TotalTime).ToString("m':'ss':'fff");

		if(Survival.TotalTime >= medals[3] && personalBest >= medals[2]) timerText.SelfModulate = LevelMenu.DIAMOND_COLOR; //Diamond (Can only be seen if you earned Gold)
		else if(Survival.TotalTime >= medals[2]) timerText.SelfModulate = LevelMenu.GOLD_COLOR;
		else if(Survival.TotalTime >= medals[1]) timerText.SelfModulate = LevelMenu.SILVER_COLOR;
		else if(Survival.TotalTime >= medals[0]) timerText.SelfModulate = LevelMenu.BRONZE_COLOR;
		else timerText.SelfModulate = Colors.White;
	}

	public override void _Process(double delta){
		if(Mode.Finished){
			SaveData();
			GD.Print("Saved");
			QueueFree();
		}
	}

	private void SaveData(){
		if(Survival.TotalTime > personalBest || float.IsNaN(personalBest)){
			Game.SaveLevelRecord(Mode.GameMode.Survival,LevelName,Survival.TotalTime);
		}
	}
}