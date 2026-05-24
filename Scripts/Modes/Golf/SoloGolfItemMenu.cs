using Godot;
using System.Collections.Generic;

public partial class SoloGolfItemMenu : Menu2D{
	private byte selectionX = 0;
	private byte selectionY = 0;
	private Sprite2D cursor;
	private Dictionary<Item,float> items;
	public static bool InMenu = false;
	private SoloGolfItemMenuOption[,] options;
	private Label juiceText;

	public override void _Ready(){
		ResetItemDictionary();
		options = new SoloGolfItemMenuOption[4,Mathf.CeilToInt(items.Count/4f)];
		
		InMenu = true;
		cursor = GetNode<Sprite2D>("ItemMenu/Cursor");
		cursor.ZIndex = 1;
		cursor.SelfModulate = Game.Players[0].PlayerColor;
		GetNode<CanvasLayer>("ItemMenu").Scale =  new Vector2(Game.Resolution / 2160f,Game.Resolution / 2160f);
		GetTree().Paused = true;
		Game.Paused = true;
		juiceText = GetNode<Label>("ItemMenu/JuiceText");
		if(!GolfCup.IsCup) GolfCup.Juice = float.PositiveInfinity;
		else GD.Print("Bruh");
		juiceText.Text = string.Format("Juice: {0:F1}",GolfCup.Juice);
		int index = 0;
		foreach(Item item in items.Keys){
			SoloGolfItemMenuOption itemOption = GD.Load<PackedScene>("res://Scenes/Object Scenes/Mode Stuff/Golf/SoloGolfItemMenu/SoloGolfItemMenuSprite.tscn").Instantiate<SoloGolfItemMenuOption>();
			itemOption.Price = items[item];
			itemOption.Item = item;
			int row = index % 4;
			int col = index / 4;
			options[row,col] = itemOption;
			itemOption.Position = new Vector2((row + 1) * 768,(col + 1) * 768);
			GetNode("ItemMenu/Selections").AddChild(itemOption);
			index++;
		}
		UpdateSelectionVisual();
		Selections = GetNode("ItemMenu/Selections").GetChildren();
	}

	public override void _Process(double delta){
		InputChecks(delta,1);
	}

	protected override void MenuChoose(int poop){
		SoloGolfItemMenuOption selection = options[selectionX,selectionY];
		if(selection.Price <= GolfCup.Juice){
			GolfCup.Juice -= selection.Price;
			Game.Players[0].Item = selection.Item;
			ResetItemDictionary();
			MenuBack();
			//SFX.Play("Item");
		}else SFX.Play("Bad");
	}

    public override void MenuBack(){
        GetTree().Paused = false;
		Game.Paused = false;
		InMenu = false;
		QueueFree();
    }

	protected override void MenuRight(){
		if(selectionX < options.GetLength(0)-1) selectionX++;
		else selectionX = 0;
		UpdateSelectionVisual();
	}

	protected override void MenuLeft(){
		if(selectionX > 0) selectionX--;
		else selectionX = (byte)(options.GetLength(0)-1);
		UpdateSelectionVisual();
	}

	protected override void MenuUp(){
		if(selectionY > 0) selectionY--;
		else selectionY = (byte)(options.GetLength(1)-1);
		UpdateSelectionVisual();
	}

	protected override void MenuDown(){
		if(selectionY < options.GetLength(1)-1) selectionY++;
		else selectionY = 0;
		UpdateSelectionVisual();
	}

    protected override void UpdateSelectionVisual(){
        cursor.Position = new Vector2((selectionX + 1) * 768,((selectionY + 1) * 768) + 256);
    }

	private void ResetItemDictionary(){
		items = new Dictionary<Item, float>{
			//Mulligan{,0.75f},
			{new Ball(Game.Players[0],1), 0.5f},
			{new BigFungus(Game.Players[0]),0.75f},
			{new BowlingBall(Game.Players[0]),2.5f},
			{new Inverter(Game.Players[0]),1.5f},
			{new Moon(Game.Players[0]),2},
			{new Pepper(Game.Players[0],1),1},
			{new SmallBall(Game.Players[0]),1},
			{new StopSign(Game.Players[0],1),1.5f},
		};
	}
}