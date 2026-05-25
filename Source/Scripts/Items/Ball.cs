using System.Collections.Generic;
using System.Linq;

public class Ball : SingleUseItem{
	public Ball(Player player,byte amount): base(player,ItemEnum.Ball,amount){}

    public override void ItemAbility(){
        if(Online.IsHost()){
            HashSet<byte> keys = new HashSet<byte>(ItemSynchronizer.SyncNode.SpawnedItems.Keys);
            byte? newItemId = ItemSynchronizer.GetUnusedItemId(keys);
            if(newItemId != null){
                ItemSynchronizer.SyncNode.Rpc(nameof(ItemSynchronizer.SyncNode.SpawnBall),Player.Id,(byte)newItemId);
            }else{ //Too many items are spawned delete oldest to make room
                byte firstItemId = ItemSynchronizer.SyncNode.SpawnedItems.ElementAt(0).Key;
                ItemSynchronizer.SyncNode.Rpc(nameof(ItemSynchronizer.SyncNode.RemoveItem),firstItemId);
                ItemSynchronizer.SyncNode.Rpc(nameof(ItemSynchronizer.SyncNode.SpawnBall),Player.Id,firstItemId);
            }
        }
	}
}