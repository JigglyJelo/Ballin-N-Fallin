public abstract class SingleUseItem : Item{
    public byte Amount;

    public SingleUseItem(Player player,ItemEnum itemType,byte Amount): base(player, itemType){
        this.Amount = Amount;
        //if(Amount > 1 && Player.Item == this) Player.ItemAmountText.Text = Amount.ToString();
    }
    
    public override void UseItem(){
		ItemAbility();
		if(Amount > 0) Amount--;
        if(Amount > 1) Player.Visuals.ItemAmountText.Text = Amount.ToString();
        else Player.Visuals.ItemAmountText.Text = "";
		if(Amount <= 0) Player.Inventory.Item = null;
	}
    public abstract void ItemAbility();
}