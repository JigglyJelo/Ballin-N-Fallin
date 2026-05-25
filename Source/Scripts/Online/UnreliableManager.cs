using System.Collections.Generic;
using Godot;

public partial class UnreliableManager{
	//For simulating the Unreliable ordered manually as the SteamMultiplayerPeer options both don't support it
	//this should on the bright side use less bandwidth as each update/sequence value is only 2 bytes and gets reset each round allowing for 18mins worth at 60 calls/sec
	private static Dictionary<UnreliableChannel,ushort> transferChannelLastUpdate = GetResetTransferChannelLastUpdate();
	private static Dictionary<UnreliableChannel,ushort> GetResetTransferChannelLastUpdate(){
		return new Dictionary<UnreliableChannel, ushort>{
			{UnreliableChannel.PlayerVelocity,0},{UnreliableChannel.PlayerPosition,0},{UnreliableChannel.PlayerArrow,0},{UnreliableChannel.PlayerDirection,0},
			{UnreliableChannel.SpawnedBoxes,0}, {UnreliableChannel.SpawnedItems,0},
			{UnreliableChannel.SportBall,0},{UnreliableChannel.TrashPosition,0},{UnreliableChannel.TrashRotation,0},
			{UnreliableChannel.Payload,0}
		};
	}
	public static void ResetTransferChannelLastUpdate(){
		transferChannelLastUpdate = GetResetTransferChannelLastUpdate();
	}
	public static bool IsNewerRpc(UnreliableChannel transferChannel, ushort sentUpdate){
		if(sentUpdate >= transferChannelLastUpdate[transferChannel]){
			transferChannelLastUpdate[transferChannel] = sentUpdate;
			return true;
		}
		return false;
	}
	public static void HostIncrementLastUpdate(UnreliableChannel transferChannel){
		if(Online.IsHost()){
			if(transferChannelLastUpdate[transferChannel] != ushort.MaxValue){
				transferChannelLastUpdate[transferChannel]++;
			}
		}else{
			GD.PrintErr("Client is attempting to increment last update");
		}
	}
	public static ushort GetChannelLastUpdate(UnreliableChannel transferChannel){
		return transferChannelLastUpdate[transferChannel];
	}
	public enum UnreliableChannel : int{
		PlayerPosition,PlayerVelocity,PlayerDirection,PlayerArrow,
		SpawnedItems, SpawnedBoxes,
		SportBall, TrashPosition, TrashRotation,
		Payload
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////
	private static Dictionary<ClientUnreliableChannel,ushort> clientsChannels = GetResetClientChannelLastUpdate();
	private static Dictionary<ClientUnreliableChannel,ushort> GetResetClientChannelLastUpdate(){
		return new Dictionary<ClientUnreliableChannel,ushort>(){{ClientUnreliableChannel.PlayerArrow,0}};
	}
	public static void ResetClientChannelLastUpdate(){
		clientsChannels = GetResetClientChannelLastUpdate();
	}
	private static Dictionary<int,Dictionary<ClientUnreliableChannel,ushort>> hostClientChannels = GetResetHostClientChannelLastUpdate();
	private static Dictionary<int,Dictionary<ClientUnreliableChannel,ushort>> GetResetHostClientChannelLastUpdate(){
		Dictionary<int,Dictionary<ClientUnreliableChannel,ushort>> channels = new Dictionary<int, Dictionary<ClientUnreliableChannel, ushort>>();
		foreach(PlayerData playerInfo in Game.PlayerDatas){
			int id = playerInfo.UUID;
			try{
				channels.Add(id,new Dictionary<ClientUnreliableChannel,ushort>(){{ClientUnreliableChannel.PlayerArrow,0}});
			}catch{}
		}
		return channels;
	}
	public static void ResetHostClientChannelLastUpdate(){
		hostClientChannels = GetResetHostClientChannelLastUpdate();
	}
	public static bool IsNewerRpc(ClientUnreliableChannel transferChannel, int senderUUID, ushort sentUpdate){
		if(sentUpdate >= hostClientChannels[senderUUID][transferChannel]){
			hostClientChannels[senderUUID][transferChannel] = sentUpdate;
			return true;
		}
		return false;
	}
	public static void ClientIncrementUpdate(ClientUnreliableChannel transferChannel){
		if(clientsChannels[transferChannel] != ushort.MaxValue){
			clientsChannels[transferChannel]++;
		}
	}
	public static ushort GetChannelLastUpdate(ClientUnreliableChannel transferChannel){
		return clientsChannels[transferChannel];
	}
	public static ushort GetChannelLastUpdate(ClientUnreliableChannel transferChannel,int senderUUID){
		return hostClientChannels[senderUUID][transferChannel];
	}
	public enum ClientUnreliableChannel : int{
		PlayerArrow
	}
}