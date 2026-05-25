using Godot;

public partial class RespawnIndicator : Node2D{
	public Label TimerText;
	public int Id;
	public override void _Ready(){
		Modulate = new Color(1,1,1,0.5f);
		TimerText = GetNode<Label>("Label");
		if(Id != 0) GetNode<Sprite2D>("BallSprite").SelfModulate = Game.Players[Id-1].PlayerColor;
	}
}