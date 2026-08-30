using Godot;
using System;

public partial class SurvivalHUD : Node{
	public static string LevelName = "";
	private Label timerText;
	private float[] medals;
	private float personalBest;
	public override void _Ready(){
		personalBest = Game.GetSavedLevelRecord(Mode.GameMode.Survival,LevelName);
		medals = (float[])Mode.ModeNode.GetNode<Level>("Level").GetMeta("medals",new float[]{0,0,0,0});
		GetNode<CanvasLayer>("CanvasLayer").Scale =  Game.ContentScaleVector2;
		timerText = GetNode<Label>("CanvasLayer/TimerText");
		GD.Print(personalBest);
	}

	public override void _PhysicsProcess(double delta){
		if(!Mode.Finished) timerText.Text = TimeSpan.FromSeconds(Survival.TotalTime).ToString("m':'ss':'fff");

		if(Survival.TotalTime < medals[3] && personalBest <= medals[2]) timerText.SelfModulate = new Color(67f/255f,103/255f,1); //Diamond (Can only be seen if you earned Gold)
		else if(Survival.TotalTime < medals[2]) timerText.SelfModulate = new Color(1,215/255f,0); //Gold
		else if(Survival.TotalTime < medals[1]) timerText.SelfModulate = new Color(192/255f,1192/255f,192/255f); //Silver
		else if(Survival.TotalTime < medals[0]) timerText.SelfModulate = new Color(205/255f, 127/255f, 50/255f); //Bronze
		else timerText.SelfModulate = Colors.White; //White
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