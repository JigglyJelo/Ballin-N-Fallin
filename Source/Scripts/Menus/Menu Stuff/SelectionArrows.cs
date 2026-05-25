using Godot;
using System;

public partial class SelectionArrows : Node2D{
    private Menu menu;
    private Polygon2D left, right;
    private ILeftRightSelections selectionInterface;
	private const int PADDING = 25;

    public override void _Ready(){
        menu = GetParent<Menu>();
        left = GetNode<Polygon2D>("LeftArrow");
        right = GetNode<Polygon2D>("RightArrow");

        // Check if the menu implements ILeftRightSelections and cast it
        selectionInterface = menu as ILeftRightSelections;
        // If the menu does not implement ILeftRightSelections, remove this node
        if(selectionInterface == null) QueueFree();
    }

    public override void _Process(double delta){
        if(selectionInterface != null && menu.Selections != null){
			Node currentSelection = menu.Selections[menu.Selection-1];
			if(currentSelection.HasMeta("setting")){
				UpdatePosition(currentSelection);
				HoveredArrow coloredArrow = IsMouseOverPolygon();
				if(Input.IsActionJustPressed("Charge N Launch Mouse") || Input.IsActionJustReleased("Charge N Launch Mouse")){
					ILeftRightSelections.ResetHold();
				}else if(Input.IsActionPressed("Charge N Launch Mouse") && Cursor.UsingCursor){
					if(ILeftRightSelections.HoldCheck()){
						switch(coloredArrow){
							case HoveredArrow.Left: selectionInterface.MenuLeft(); break;
							case HoveredArrow.Right: selectionInterface.MenuRight(); break;
							case HoveredArrow.None: ILeftRightSelections.ResetHold(); return; //Avoids updating with delta
						}
					}
					ILeftRightSelections.UpdateHold((float)delta);
				}
			}else{
				Position = new Vector2(0,10000);
				Visible = false;
			}
        }
    }

    private HoveredArrow IsMouseOverPolygon(){
		if(Geometry2D.IsPointInPolygon(left.GetLocalMousePosition(), left.Polygon)){
			left.Color = Menu.SELECTED_COLOR;
			right.Color = Colors.White;
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			return HoveredArrow.Left;
		}else if(Geometry2D.IsPointInPolygon(right.GetLocalMousePosition(), right.Polygon)){
			left.Color = Colors.White;
			right.Color = Menu.SELECTED_COLOR;
			Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
			return HoveredArrow.Right;
		}else{
			left.Color = Colors.White;
			right.Color = Colors.White;
			return HoveredArrow.None;
		}
    }

	private void UpdatePosition(Node selection){
		if(selection is Node2D selection2D){
			if(GlobalPosition != selection2D.GlobalPosition){
				GlobalPosition = selection2D.GlobalPosition;
				Visible = true;
			}
		}else if(selection is Label label){
			if(GlobalPosition != label.GlobalPosition){
				float halfLength = (label.Size.X / 2f)*label.Scale.X;
				GlobalPosition = label.GlobalPosition;
				if(label.PivotOffset.X == 0){ //Left
					GlobalPosition += new Vector2(halfLength,0);
				}else if(MathF.Abs(label.PivotOffset.X-halfLength) < 50){ //Right
					GlobalPosition -= new Vector2(halfLength,0);
				}
				float halfHeight = (label.Size.Y / 2f)*label.Scale.Y;
				if(label.PivotOffset.Y == 0){ //Top
					GlobalPosition += new Vector2(0,halfHeight);
				}else if(MathF.Abs(label.PivotOffset.Y-halfHeight) < 50){ //Bottom
					GlobalPosition -= new Vector2(0,halfHeight);
				}
				
				left.Position = new Vector2(-halfLength-PADDING,0);
				right.Position = new Vector2(halfLength+PADDING,0);
				Visible = true;
			}
		}
	}

	private enum HoveredArrow{
		Left,Right,None
	}
}