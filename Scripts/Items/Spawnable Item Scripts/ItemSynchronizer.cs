using System;
using Godot;
using System.Collections.Generic;

public partial class ItemSynchronizer : Node{
	public static ItemSynchronizer SyncNode;
	private const int ITEM_SYNC_RATE = 2;
    private int itemSyncTimer = 0;
    private const int ITEM_BOX_SYNC_RATE = 4;
	private int boxSyncTimer = 0;
    //Up to 256 items can be spawned at once since each need a unique byte Id
    public Dictionary<byte, ItemBox> SpawnedBoxes = new Dictionary<byte, ItemBox>();
    public Dictionary<byte,SpawnableItemScript> SpawnedItems = new Dictionary<byte, SpawnableItemScript>();
	public override void _Ready(){
		SyncNode = this;
        SetProcess(false);
	}

	public override void _PhysicsProcess(double delta){
        if(Online.PeerIsActive() && Online.IsHost() && Online.IsOnline){
			if(++itemSyncTimer == ITEM_SYNC_RATE){
				SendItemInfo();
				itemSyncTimer = 0;
			}
            if(++boxSyncTimer == ITEM_BOX_SYNC_RATE){
                SendBoxInfo();
                boxSyncTimer = 0;
            }
		}
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void SyncBoxes(byte[] boxData){
        ushort update = BitConverter.ToUInt16(boxData, boxData.Length-2);
        if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.SpawnedBoxes,update)){
            int offset = 0;
            while(offset < boxData.Length-2){
                try{
                    byte id = boxData[offset++];
                    float posX = BitConverter.ToSingle(boxData, offset); offset += 4;
                    float posY = BitConverter.ToSingle(boxData, offset); offset += 4;
                    float velX = BitConverter.ToSingle(boxData, offset); offset += 4;
                    float velY = BitConverter.ToSingle(boxData, offset); offset += 4;
                    
                    if(SpawnedBoxes.TryGetValue(id, out ItemBox box)){
                        // If your ItemBox RigidBody uses the same custom NetworkPosition/Velocity logic as your Balls,
                        // change these to box.Rb.NetworkPosition / box.Rb.NetworkVelocity
                        box.Rb.NetworkPosition = new Vector2(posX, posY);
                        box.Rb.NetworkVelocity = new Vector2(velX, velY);
                    }
                }catch{}
            }
        }
    }

    public void SendBoxInfo(){
        if(SpawnedBoxes.Count > 0){
            List<byte> boxIds = new List<byte>();
            List<Vector2> boxPos = new List<Vector2>();
            List<Vector2> boxVel = new List<Vector2>();

            foreach(KeyValuePair<byte, ItemBox> item in SpawnedBoxes){
                if(item.Value != null && IsInstanceValid(item.Value) && item.Value.Rb != null){
                    boxIds.Add(item.Key);
                    boxPos.Add(item.Value.Rb.GlobalPosition);
                    boxVel.Add(item.Value.Rb.LinearVelocity);
                }
            }

            if(boxIds.Count > 0){
                int dataSize = boxIds.Count * 17;
                byte[] boxData = new byte[dataSize+2];
                int offset = 0;

                for(int i = 0; i < boxIds.Count; i++){
                    boxData[offset++] = boxIds[i];
                    Buffer.BlockCopy(BitConverter.GetBytes(boxPos[i].X), 0, boxData, offset, 4); offset += 4;
                    Buffer.BlockCopy(BitConverter.GetBytes(boxPos[i].Y), 0, boxData, offset, 4); offset += 4;
                    Buffer.BlockCopy(BitConverter.GetBytes(boxVel[i].X), 0, boxData, offset, 4); offset += 4;
                    Buffer.BlockCopy(BitConverter.GetBytes(boxVel[i].Y), 0, boxData, offset, 4); offset += 4;
                }

                ushort update = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.SpawnedBoxes);
                BitConverter.GetBytes(update).CopyTo(boxData, boxData.Length-2);
                Rpc(nameof(SyncBoxes), boxData);
            }
        }
    }

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnBall(byte playerId,byte id){
        BallScript ball = GD.Load<PackedScene>("res://Scenes/Object Scenes/Items/Ball.tscn").Instantiate<BallScript>();
        ball.SetMultiplayerAuthority(1);
        ball.Player = Game.Players[playerId-1];
        ball.Name = "SpawnedItem" + id;
        ball.Rb = ball.GetNode<InterpolatedBody>("RigidBody2D");
        ball.Rb.GlobalPosition = ball.Player.Rb.GlobalPosition;
        ball.Rb.NetworkPosition = ball.Player.Rb.GlobalPosition;
        SpawnedItems.Add(id,ball);
        AddChild(ball);
    }

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]//Ordered, TransferChannel = (int)Online.TransferChannelEnum.SpawnedItems
    private void SyncBalls(byte[] ballData){
        ushort update = BitConverter.ToUInt16(ballData, ballData.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.SpawnedItems,update)){
            int offset = 0;
            while(offset < ballData.Length-2){
                try{
                    byte id = ballData[offset++];
                    float posX = BitConverter.ToSingle(ballData, offset); offset += 4;
                    float posY = BitConverter.ToSingle(ballData, offset); offset += 4;
                    float velX = BitConverter.ToSingle(ballData, offset); offset += 4;
                    float velY = BitConverter.ToSingle(ballData, offset); offset += 4;
                    if(SpawnedItems.TryGetValue(id, out SpawnableItemScript item) && item is BallScript ball){
                        //ball.Rb.GlobalPosition = new Vector2(posX, posY);
                        //ball.Rb.GlobalPosition = ball.Rb.GlobalPosition; // Fix for quantum superposition
                        //ball.Rb.LinearVelocity = new Vector2(velX, velY);
                        ball.Rb.NetworkPosition = new Vector2(posX, posY);
                        ball.Rb.NetworkVelocity = new Vector2(velX, velY);
                    }
                }catch{}
            }
        }
    }


    public void SendItemInfo(){
        if(SpawnedItems.Count > 0){
            List<byte> ballIds = new List<byte>();
            List<Vector2> ballPos = new List<Vector2>();
            List<Vector2> ballVel = new List<Vector2>();

            foreach(KeyValuePair<byte, SpawnableItemScript> item in SpawnedItems){
                if(item.Value is BallScript ball){
                    ballIds.Add(item.Key);
                    ballPos.Add(ball.Rb.GlobalPosition);
                    ballVel.Add(ball.Rb.LinearVelocity);
                }
            }

            if(ballIds.Count > 0){
                int dataSize = ballIds.Count * 17;
                byte[] ballData = new byte[dataSize+2];
                int offset = 0;

                for(int i = 0; i < ballIds.Count; i++){
                    ballData[offset++] = ballIds[i];
                    Buffer.BlockCopy(BitConverter.GetBytes(ballPos[i].X), 0, ballData, offset, 4);
                    offset += 4;
                    Buffer.BlockCopy(BitConverter.GetBytes(ballPos[i].Y), 0, ballData, offset, 4);
                    offset += 4;
                    Buffer.BlockCopy(BitConverter.GetBytes(ballVel[i].X), 0, ballData, offset, 4);
                    offset += 4;
                    Buffer.BlockCopy(BitConverter.GetBytes(ballVel[i].Y), 0, ballData, offset, 4);
                    offset += 4;
                }
                ushort update = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.SpawnedItems);
                BitConverter.GetBytes(update).CopyTo(ballData, ballData.Length-2);
                Rpc(nameof(SyncBalls), ballData);
            }
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveItem(byte itemId){
        SpawnableItemScript item = SpawnedItems[itemId];
        if(item is BallScript ball) ParticleManager.SpawnPopParticles(ball.Rb.GlobalPosition, ball.GetNode<Sprite2D>("Smoothing2D/BallSprite").SelfModulate);
        SpawnedItems.Remove(itemId);
        item.QueueFree();
    }
    
    //Returns the first byte value not present in the hashset
    //If all values (0-255) are used null is returned
    public static byte? GetUnusedItemId(HashSet<byte> keys){
        int keyCount = keys.Count;
        if(keyCount > byte.MaxValue) return null;
        byte newItemId = (byte)keyCount;
        if(keyCount != 0){
            int attempts = 0;
            while(keys.Contains(newItemId)){
                newItemId++;
                attempts++;
                if(attempts == byte.MaxValue) return null;
            }
        }
        return newItemId;
    }
}