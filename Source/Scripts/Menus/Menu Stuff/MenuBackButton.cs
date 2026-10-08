using Godot;

public partial class MenuBackButton : Label{
	private bool hovered = false;

	public override void _Process(double delta){
		if(hovered && Visible){
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			if(Input.IsActionJustReleased("Charge N Launch Mouse")){
				Node current = GetParent();
				while(current != null){
					if(current is Menu menu){
						menu.MenuBack();
						break;
					}
					current = current.GetParent();
				}
			}
		}
	}

	public void _on_mouse_entered(){
		hovered = true;
		if(Visible){
			Modulate = Menu.SELECTED_COLOR;
			SFX.Play("Move");
		}
	}

	public void _on_mouse_exited(){
		hovered = false;
		Modulate = Colors.White;
	}
}