using Godot;

public partial class GolfHUD : Node{
	public static string LevelName = "";
	private Label parText,strokesText;
	private float timer = 0;
	private float lockout = 0.25f;
	private float personalBestPar;
	public override void _Ready(){
		personalBestPar = Game.GetSavedLevelRecord(Mode.GameMode.Golf,LevelName);
		GetNode<CanvasLayer>("CanvasLayer").Scale =  Game.ContentScaleVector2;
		parText = GetNode<Label>("CanvasLayer/ParText");
		strokesText = GetNode<Label>("CanvasLayer/StrokesText");
		Color color = Game.PlayerDatas[0].PlayerColor;
		strokesText.SelfModulate = color;
		if(Game.TotalPlayers == 1) parText.SelfModulate = color;
		parText.Text = "Par: " + Golf.Par;
		if(Game.TotalPlayers > 1) strokesText.Visible = false;
	}

	public override void _Process(double delta){
		if(strokesText.Text != "Strokes: " + Golf.PlayerStrokes[0]) strokesText.Text = "Strokes: " + Golf.PlayerStrokes[0];
		if(Mode.Finished){
			SaveData();
			QueueFree();
		}
		if(Game.Players[0].Inventory.Item == null){
			timer += (float)delta;
			if(Input.IsActionJustPressed("Item1") && Game.TotalPlayers == 1 && !SoloGolfItemMenu.InMenu && timer >= lockout){
				AddChild(GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Golf/SoloGolfItemMenu/SoloGolfItemMenu.tscn").Instantiate());
				SoloGolfItemMenu.InMenu = true;
				timer = 0;
			}
		}
	}

	private void SaveData(){
		if(float.IsNaN(personalBestPar) || Golf.TopStroke < (int)personalBestPar){
			Game.SaveLevelRecord(Mode.GameMode.Golf,LevelName,Golf.TopStroke);
		}
	}
}