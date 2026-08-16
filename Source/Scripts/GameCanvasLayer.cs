using Godot;

public partial class GameCanvasLayer : CanvasLayer{
	public override void _Ready(){
		Scale = Game.ContentScaleVector2;
	}
}