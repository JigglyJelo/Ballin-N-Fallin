using Godot;
using System;
using System.Linq;

public abstract partial class SpawnableItemScript : Node2D{
	protected float lifetime;
    protected float timer = 0;
    public static byte SpawnedItemId = 0;
    public override void _PhysicsProcess(double delta){
        timer += (float)delta;
        if(Online.IsHost()){
            if(timer >= lifetime){
                DeleteItem();
            } 
        }
    }

    protected void DeleteItem(){
        try{
            byte key = ItemSynchronizer.SyncNode.SpawnedItems.FirstOrDefault(x => x.Value == this).Key;
            ItemSynchronizer.SyncNode.Rpc(nameof(ItemSynchronizer.SyncNode.RemoveItem),key);
        }catch(Exception ex){
            GD.PrintErr(Name.ToString() + " " + ex.ToString());
        }
    }
}