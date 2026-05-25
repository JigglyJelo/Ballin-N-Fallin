using Godot;

public partial class Cursor : Node{
	public static Input.CursorShape CursorThisFrame = Input.CursorShape.Arrow;
	public static bool UsingCursor = false;
	
    public override void _PhysicsProcess(double delta){
		if(Input.MouseMode != Input.MouseModeEnum.Hidden){
			Input.SetDefaultCursorShape(CursorThisFrame);
        	if(Input.GetLastMouseVelocity() != Vector2.Zero) CursorThisFrame = Input.CursorShape.Arrow;
		}
    }
}
