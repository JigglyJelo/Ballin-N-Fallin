using Godot;

public abstract class Item{
	public const string ICON_PATH = "res://Assets/Sprites/Items/Small/";
	public Player Player;
	public string ItemName;
	public ItemEnum ItemType;
	public Texture2D Icon;

	public Item(Player player,ItemEnum itemType){
		Player = player;
		ItemType = itemType;
		ItemName = EnumToString(ItemType);
		Icon = GetIcon();
	}

	public Texture2D GetIcon(){
		return GD.Load<Texture2D>(ICON_PATH + ItemName + ".png");
	}
	public abstract void UseItem();
	public enum ItemEnum{
		Ball,BigFungus,Booll,BowlingBall,Inverter,Moon,Pepper,SmallBall,StopSign,Wings
	}
	public string EnumToString(ItemEnum item){
		switch(item){
			case ItemEnum.Ball: return "Ball";
			case ItemEnum.BigFungus: return "Big Fungus";
			case ItemEnum.Booll: return "Booll";
			case ItemEnum.BowlingBall: return "Bowling Ball";
			case ItemEnum.Inverter: return "Inverter";
			case ItemEnum.Moon: return "Moon";
			case ItemEnum.Pepper: return "Pepper";
			case ItemEnum.SmallBall: return "Small Ball";
			case ItemEnum.StopSign: return "Stop Sign";
			case ItemEnum.Wings: return "Wings";
		}
		return null;
	}
}