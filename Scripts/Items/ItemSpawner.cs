using Godot;

public partial class ItemSpawner : Sprite2D{
	public static byte TotalSpawners;
	public byte SpawnerId;
	public bool ItemSpawned = false;
	private static PackedScene itemBoxScene;
	private ItemBox itemBoxNode;
	private float spawnTime;
	private float timer;
	private AudioStreamPlayer2D breakSound;
	private CpuParticles2D breakParticles;

	public override void _Ready(){
		SpawnerId = TotalSpawners;
		TotalSpawners++;
		Texture = null;
		if(itemBoxScene == null) itemBoxScene = GD.Load<PackedScene>("res://Scenes/Object Scenes/Items/ItemBox.tscn");
		if(Online.IsHost()) spawnTime = Game.Random.Next(3,13);
		breakSound = GetNode<AudioStreamPlayer2D>("SFX");
		float zoomScale = 1 + (1-(GetParent() as Level).CameraZoom);
		breakSound.MaxDistance = 2304*zoomScale;
		breakParticles = GetNode<CpuParticles2D>("Particles");
	}

	public override void _PhysicsProcess(double delta){
		if((Online.PeerIsActive() && Online.IsHost()) || !Online.IsOnlinePeer()){
			if(!ItemSpawned){
				timer += (float)delta;
				if(timer >= spawnTime && itemsEnabled()){
					Rpc(nameof(SpawnItemBox));
				}
			}
		}

		bool itemsEnabled(){
			return Tour.CurrentTour.ItemsEnabled && !(Game.TotalPlayers == 1 && (Game.CurrentMode == Mode.GameMode.Race || Game.CurrentMode == Mode.GameMode.Golf || Game.CurrentMode == Mode.GameMode.Survival));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SpawnItemBox(){
		ItemSpawned = true;
		itemBoxNode = itemBoxScene.Instantiate<ItemBox>();
		itemBoxNode.Creator = this;
		itemBoxNode.Rb = itemBoxNode.GetNode<InterpolatedBody>("RigidBody2D");
        itemBoxNode.Rb.GlobalPosition = GlobalPosition;
        itemBoxNode.Rb.NetworkPosition = GlobalPosition;
		Mode.ModeNode.AddChild(itemBoxNode);
		if(ItemSynchronizer.SyncNode != null){
            ItemSynchronizer.SyncNode.SpawnedBoxes[SpawnerId] = itemBoxNode;
        }
		if(Online.IsHost()){
			int minTime = 6 - TotalSpawners;
			if(minTime < 1) minTime = 1;
			int maxTime = 7 * TotalSpawners;
			if(maxTime <= minTime) maxTime = minTime + 1;
			spawnTime = Game.Random.Next(minTime,maxTime);
			timer = 0;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RemoveItemBox(bool playSFX){
		if(itemBoxNode != null){
			if(playSFX){
				breakParticles.GlobalPosition = itemBoxNode.Rb.GlobalPosition;
				breakParticles.Emitting = true;
				breakSound.Play();
			}
			if(ItemSynchronizer.SyncNode != null){
                ItemSynchronizer.SyncNode.SpawnedBoxes.Remove(SpawnerId);
            }
			ItemSpawned = false;
			itemBoxNode.QueueFree();
			itemBoxNode = null;
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RemoveItemBox(){
		RemoveItemBox(false);
	}

	//Calculates Item on host and sends result to all players
	public void SendItemToPlayer(byte playerId){
		if(Online.IsHost()){
			//Get item
			Player player = Game.Players[playerId-1];
			Item item = Mode.ModeNode.GiveItem(player);
			//Send resulting item to other players
			if(item is SingleUseItem) player.Rpc(nameof(player.SetItemFromEnum), (byte)item.ItemType, (item as SingleUseItem).Amount);
			else player.Rpc(nameof(player.SetItemFromEnum), (byte)item.ItemType);
		}
	}
}