using Godot;

public partial class ModeSetter : Node{
	public override void _Ready(){
		string filePath = "";
		switch(Game.CurrentMode){
			case Mode.GameMode.KingOfTheHill: filePath = "res://Source/Scripts/Modes/King of the Hill/KOTH.cs"; break;
			case Mode.GameMode.CrownTheKing: filePath = "res://Source/Scripts/Modes/Crown the King/CTK.cs"; break;
			case Mode.GameMode.BallinToTheBank: filePath = "res://Source/Scripts/Modes/Ballin to the Bank/BTTB.cs"; break;
			default: filePath = "res://Source/Scripts/Modes/" + Mode.EnumToString(Game.CurrentMode) + "/" + Mode.EnumToString(Game.CurrentMode).Replace(" ","") + ".cs"; break;
		}
		
		if(!string.IsNullOrEmpty(filePath)) GetParent().SetScript(GD.Load(filePath));
		QueueFree();
	}
}