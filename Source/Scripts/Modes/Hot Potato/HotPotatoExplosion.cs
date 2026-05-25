using Godot;

public partial class HotPotatoExplosion : AnimatedSprite2D{
	public static Vector2 DeathPoint;
	public override void _Ready(){
		SFX.Play("Explosion",GlobalPosition);
		DeathPoint = GlobalPosition;
	}

	public override void _Process(double delta){
		if(Frame == 4) QueueFree();
	}
}