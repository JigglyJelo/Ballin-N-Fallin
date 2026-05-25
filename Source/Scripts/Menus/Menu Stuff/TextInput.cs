using Godot;
using System;

public partial class TextInput : Polygon2D{
	[Export]
	private InputType allowedKeys = InputType.Letters;
	[Export]
	private string startingString = "";
	[Export]
	private string placeholderText = "";
	[Export]
	private int maxLength = 16;
	private string inputString = "";
	private bool selectAll = false;
	public string InputString{
		get{return inputString;}
		set{
			inputString = value;
			if(inputString.Equals("")){
				inputLabel.Text = placeholderText;
				canvasGroup.SelfModulate = new Color(1,1,1,0.5f);
			}else{
				inputLabel.Text = inputString;
				canvasGroup.SelfModulate = Colors.White;
			}
		}
	}
	public bool Selected = false;
	private Label inputLabel;
	private CanvasGroup canvasGroup;
	private Line2D outline;
	[Signal]
	public delegate void TextUpdatedEventHandler(string text);

	public override void _Ready(){
		canvasGroup = GetNode<CanvasGroup>("CanvasGroup");
		inputLabel = canvasGroup.GetNode<Label>("InputLabel");
		Vector2 size = new Vector2(MathF.Abs(Polygon[0].X) + MathF.Abs(Polygon[1].X), MathF.Abs(Polygon[1].Y) + MathF.Abs(Polygon[2].Y));
		inputLabel.Size = size;
		outline = GetNode<Line2D>("Outline");
		outline.Points = Polygon;
		InputString = startingString;
	}

	public override void _Process(double delta){
		if(Input.IsActionJustReleased("Charge N Launch Mouse")){
			Selected = Menu.IsMouseOverPolygon(this);
			outline.DefaultColor = Selected ? new Color(0.5f,0.5f,0.5f) : Colors.Black;
			if(!Selected) SetSelectAll(false);
		}else if(Menu.IsMouseOverPolygon(this)){
			Cursor.CursorThisFrame = Input.CursorShape.Ibeam;
		}
	}

	public override void _Input(InputEvent @event){
		if(Selected){
			if(@event is InputEventKey keyEvent && keyEvent.Pressed){
				Key key = keyEvent.Keycode;
				if(Input.IsKeyPressed(Key.Ctrl)){
					if(key == Key.V && DisplayServer.ClipboardHas()){
						string clipboardString = DisplayServer.ClipboardGet();
						InputString = clipboardString.Length > maxLength ? clipboardString.Substring(0,maxLength) : clipboardString;
						EmitSignal(SignalName.TextUpdated, InputString);
					}else if(key == Key.A && !InputString.Equals("")){
						SetSelectAll(!selectAll);
					}else if(selectAll && key == Key.C){
						DisplayServer.ClipboardSet(InputString);
						if(selectAll) SetSelectAll(false);
					}
				}else if(IsKeyAllowed(key) && InputString.Length < maxLength){
					if(selectAll) SetSelectAll(false);
					char inputChar = (char)key;
					if(Input.IsKeyPressed(Key.Shift)) InputString += inputChar;
					else InputString += char.ToLower(inputChar);
					EmitSignal(SignalName.TextUpdated, InputString);
        		}else if(key == Key.Backspace){
					if(selectAll){
						if(selectAll) SetSelectAll(false);
						InputString = "";
						EmitSignal(SignalName.TextUpdated, InputString);
					}else if(InputString.Length > 0){
						InputString = InputString.Substring(0,InputString.Length-1);
						EmitSignal(SignalName.TextUpdated, InputString);
					}
				}
    		}
		}
	}

	private enum InputType{
		Letters, Numbers, Username, IP, Any
	}

	private bool IsKeyAllowed(Key key){
		switch(allowedKeys){
			case InputType.Letters: return key >= Key.A && key <= Key.Z;
			case InputType.Numbers: return key >= Key.Key0 && key <= Key.Key9;
			case InputType.Username: return (key >= Key.A && key <= Key.Z) || (key >= Key.Key0 && key <= Key.Key9) || key == Key.Space;
			case InputType.IP: return (key >= Key.Key0 && key <= Key.Key9) || (key >= Key.A && key <= Key.F) || key == Key.Period;
			case InputType.Any: return key != Key.Backspace;
			default: return false;
		}
	}

	private void SetSelectAll(bool selectAll){
		this.selectAll = selectAll;
		Color = selectAll ? new Color(0,0,1,0.5f) : new Color(Colors.Black, 0.5f);
	}
}
