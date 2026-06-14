using Godot;

public partial class MenuBackButton : Node2D{
	public const int LEFT = -380;
	public const int RIGHT = 280;
	public const int TOP = -70;
	public const int BOTTOM = 70;
	private bool hovered;

    public override void _PhysicsProcess(double delta){
		if(Visible){
			Vector2 mousePosition = GetLocalMousePosition();
			if(mousePosition.X > LEFT && mousePosition.X < RIGHT && mousePosition.Y > TOP && mousePosition.Y < BOTTOM){
				if(!hovered){
					hovered = true;
					Modulate = Menu.SELECTED_COLOR;
					SFX.Play("Move");
				}
				if(hovered){
					Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
				}
				if(Input.IsActionJustReleased("Charge N Launch Mouse") && GetParent() is Menu menu){
					menu.MenuBack();
				}
			}else{
				if(hovered){
					hovered = false;
					Modulate = Colors.White;
				}
			}
		}
	}
}
