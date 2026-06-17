using Godot;

public class PlayerInventory{
    private Player player;
    private Item item = null;
    public float ItemRouletteTimer;

    public PlayerInventory(Player player) {
        this.player = player;
    }

    public Item Item {
        get { return item; }
        set {
            item = value;
            player.Visuals.SetItemSpriteTexture();
        }
    }

    public void UpdateInventory(float delta) {
        // Transformation Timer and special abilities
        if (Item is TransformItem) {
            TransformItem tItem = (TransformItem)Item;
            tItem.TransformItemTimer(delta);
            
            if (tItem.Activated) {
                if (tItem is Wings) {
                    player.CanLaunch = true;
                } else if (tItem is Moon && player.Physics.AirTimer >= Mode.ModeNode.MoonAirTimeRequirement) {
                    player.CanLaunch = true;
                    player.Physics.AirTimer = 0;
                }
            }
        }
    }

    public void ItemButtonPressed() {
        if (Item != null && !player.Visuals.ItemRouletteAnimation.Visible) {
            player.RpcId(1, nameof(player.ClientSendUseItem));
        }
    }

    public void SetItemFromEnum(byte itemEnum) {
        switch ((Item.ItemEnum)itemEnum) {
            case Item.ItemEnum.BigFungus: Item = new BigFungus(player); break;
            case Item.ItemEnum.Booll: Item = new Booll(player); break;
            case Item.ItemEnum.BowlingBall: Item = new BowlingBall(player); break;
            case Item.ItemEnum.Inverter: Item = new Inverter(player); break;
            case Item.ItemEnum.Moon: Item = new Moon(player); break;
            case Item.ItemEnum.SmallBall: Item = new SmallBall(player); break;
            case Item.ItemEnum.Wings: Item = new Wings(player); break;
        }
    }

    public void SetItemFromEnum(byte itemEnum, byte amount) {
        switch ((Item.ItemEnum)itemEnum) {
            case Item.ItemEnum.Ball: Item = new Ball(player, amount); break;
            case Item.ItemEnum.Pepper: Item = new Pepper(player, amount); break;
            case Item.ItemEnum.StopSign: Item = new StopSign(player, amount); break;
        }
    }

    public void HandleClientSendUseItem() {
        if (Online.IsHost()) {
            if (player.IsRpcFromPlayerOwner()) {
                if (Item is SingleUseItem suItem) {
                    player.Rpc(nameof(player.HostSentUseItem), (byte)Item.ItemType, suItem.Amount);
                } else if (Item != null) {
                    player.Rpc(nameof(player.HostSentUseItem), (byte)Item.ItemType);
                }
            } else {
                GD.PrintErr(OnlineErrorMessages.ClientSpoofErrorMessage(player.OwnerId));
            }
        } else {
            GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
        }
    }

    public void HandleHostSentUseItem(byte itemEnum) {
        if (Item.ItemType == (Item.ItemEnum)itemEnum) {
            Item.UseItem();
        } else {
            GD.PrintErr("Item desync from host");
            SetItemFromEnum(itemEnum);
            Item.UseItem();
        }
    }

    public void HandleHostSentUseItem(byte itemEnum, byte amount) {
        if (Item.ItemType == (Item.ItemEnum)itemEnum && (Item as SingleUseItem).Amount == amount) {
            Item.UseItem();
        } else {
            GD.PrintErr("Item desync from host");
            SetItemFromEnum(itemEnum, amount);
            Item.UseItem();
        }
    }
}