using Godot;
using System.Collections.Generic;
using System.Text;

public partial class InputPrompts : RichTextLabel{
	private static readonly Dictionary<InputPrompt,string> InputImages = new(){
		{ InputPrompt.North, "res://Assets/Sprites/Input Prompts/North.png" },
		{ InputPrompt.South, "res://Assets/Sprites/Input Prompts/South.png" },
		{ InputPrompt.East, "res://Assets/Sprites/Input Prompts/East.png" },
		{ InputPrompt.West, "res://Assets/Sprites/Input Prompts/West.png" },
		{ InputPrompt.LT, "res://Assets/Sprites/Input Prompts/LT Prompt.png" },
		{ InputPrompt.RT, "res://Assets/Sprites/Input Prompts/RT Prompt.png" },
		{ InputPrompt.Joystick, "res://Assets/Sprites/Input Prompts/LeftStick.png" }
	};
	[Export]
	private Godot.Collections.Dictionary<InputPrompt,string> InputMessages = new(){
		{ InputPrompt.South, "Confirm" },
		{ InputPrompt.East, "Back" },
	};
		
	public enum InputPrompt{
		North, West, South, East, LT, RT, Joystick
	}
	public override void _Ready(){
		int fontSize = GetThemeFontSize("normal_font_size");
		StringBuilder finalText = new StringBuilder();
		finalText.Append("[right]");
		foreach(InputPrompt prompt in InputMessages.Keys){
			finalText.Append($"[img={fontSize}]{InputImages[prompt]}[/img] {InputMessages[prompt]}   ");
		}
		finalText.Append("[/right]");
		Text = finalText.ToString();
	}
}