using Godot;

public partial class Countdown : Node{
	private const float TIME = 3;
	public const float RACE_START_TIMESCALE = 0.01f;
	private float timer = TIME;
	private Label countdownText;

	public override void _Ready(){
		GetNode<CanvasLayer>("CanvasLayer").Scale = Game.ContentScaleVector2;
		countdownText = GetNode<Label>("CanvasLayer/Countdown");
		if(!Online.IsOnline){
			Engine.TimeScale = RACE_START_TIMESCALE;
		}
		countdownText.Text = TIME.ToString();
	}

	public override void _PhysicsProcess(double delta){
		timer -= (float)delta * 100;
		countdownText.Text = Mathf.CeilToInt(timer).ToString();
		if(timer <= 0){
			Engine.TimeScale = 1;
			QueueFree();
		}
	}
}