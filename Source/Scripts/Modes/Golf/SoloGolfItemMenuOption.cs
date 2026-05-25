using Godot;

public partial class SoloGolfItemMenuOption : Sprite2D{
	public float Price;
	public Item Item;
	public override void _Ready(){
		Texture = GD.Load<Texture2D>("res://Assets/Sprites/Items/" + Item.ItemName + ".png");
		GetNode<Label>("ItemCostText").Text = string.Format("{0:F1}",Price);
	}
}