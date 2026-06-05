using Godot;

public partial class EndBackgroundTransition : CanvasLayer{
	private Polygon2D background;
	private AudioStreamPlayer music;
	public override void _Ready(){
		music = Game.GameNode.GetNode<AudioStreamPlayer>("Scene/MusicPlayer");
		Scale = Game.ContentScaleVector2;
		//Fade out Music
		Tween musicTween = GetTree().CreateTween();
		musicTween.TweenProperty(music,"volume_db",-60,0.75);
		//Fade in Background
		Game.GameNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Backgrounds/MenuBackgroundLayer.tscn").Instantiate());
		background = Game.GameNode.GetNode<Polygon2D>("BackgroundLayer/Background");
		background.SelfModulate = Game.CLEAR;
		CanvasLayer bgLayer = background.GetParent<CanvasLayer>();
		bgLayer.FollowViewportEnabled = false;
		float expectedScale = Game.ContentScaleVector2.X;
		bgLayer.Scale = new Vector2(expectedScale, expectedScale);
		bgLayer.Offset = new Vector2(3840f*expectedScale/2,2160f*expectedScale/2);
		Tween opacityTween = GetTree().CreateTween();
		opacityTween.TweenProperty(background,"self_modulate",Colors.White,0.75);
		opacityTween.TweenCallback(Callable.From(SwitchToScoreScreen));
	}

	//Called when background has fully faded in
	private void SwitchToScoreScreen(){
		background.SelfModulate = Colors.White;
		foreach(Node node in Mode.ModeNode.GetChildren()){
			if(node is Node2D node2D) node2D.Visible = false;
		}
		SceneTransitioner.SwitchToScene(Game.SceneType.ScoreScreen,false);
	}
}