using Godot;
using System;

public partial class RaceHUD : Node{
	public static string LevelName = "";
	private Label raceTimerText,lapText;
	private float[] medals;
	private float personalBest;
	public override void _Ready(){
		Game.Save.Load(Game.SAVE_PATH);
		LoadData();
		medals = (float[])Mode.ModeNode.GetNode<Level>("Level").GetMeta("medals",new float[]{0,0,0,0});
		GetNode<CanvasLayer>("CanvasLayer").Scale = Game.ContentScaleVector2;
		raceTimerText = GetNode<Label>("CanvasLayer/TimerText");
		lapText = GetNode<Label>("CanvasLayer/LapText");
		lapText.SelfModulate = Game.PlayerDatas[0].PlayerColor;
		GD.Print(personalBest);
	}

	public override void _PhysicsProcess(double delta){
		if(!Mode.Finished) raceTimerText.Text = TimeSpan.FromSeconds(Race.RaceTimer).ToString("m':'ss':'fff");

		if(Race.RaceTimer < medals[3] && personalBest <= medals[2]) raceTimerText.SelfModulate = new Color(67f/255f,103/255f,1); //Diamond (Can only be seen if you earned Gold)
		else if(Race.RaceTimer < medals[2]) raceTimerText.SelfModulate = new Color(1,215/255f,0); //Gold
		else if(Race.RaceTimer < medals[1]) raceTimerText.SelfModulate = new Color(192/255f,1192/255f,192/255f); //Silver
		else if(Race.RaceTimer < medals[0]) raceTimerText.SelfModulate = new Color(205/255f, 127/255f, 50/255f); //Bronze
		else raceTimerText.SelfModulate = Colors.White; //White

		if(Race.PlayerLaps.Length > 0 && !lapText.Text.Equals("Lap " + (Race.PlayerLaps[0] + 1) + "/" + Race.TotalLaps)){
			lapText.Text = "Lap " + (Race.PlayerLaps[0] + 1) + "/" + Race.TotalLaps;
		}
	}

    public override void _Process(double delta){
        if(Mode.Finished){
			SaveData();
			GD.Print("Saved");
			QueueFree();
		} 
    }

	private void LoadData(){
		personalBest = (float)Game.Save.GetValue("Time Trials",LevelName, float.MaxValue);
	}

	private void SaveData(){
		if(Race.RaceTimer < personalBest){
			Game.Save.SetValue("Time Trials",LevelName, Race.RaceTimer);
			Game.Save.Save(Game.SAVE_PATH);
		} 
	}
}