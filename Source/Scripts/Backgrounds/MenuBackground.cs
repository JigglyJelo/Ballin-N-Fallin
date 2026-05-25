using Godot;

public partial class MenuBackground : Polygon2D{
    public const float SPEED = 50;
	public static Vector2 StartPoint;
    public override void _Ready(){
        TextureOffset = StartPoint;
    }
    public override void _Process(double delta){
		float distance = SPEED * (float)delta / (float)Engine.TimeScale;
		TextureOffset -= new Vector2(distance,distance);
	}
}