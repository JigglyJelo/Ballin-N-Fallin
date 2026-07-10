using System.Collections.Generic;
using Godot;

public partial class UnreliableManager{
	//For simulating the Unreliable ordered manually as the SteamMultiplayerPeer options both don't support it
	//this should on the bright side use less bandwidth as each update/sequence value is only 2 bytes
	private static Dictionary<UnreliableChannel,ushort> transferChannelLastUpdate = GetResetTransferChannelLastUpdate();
	private static Dictionary<UnreliableChannel,ushort> GetResetTransferChannelLastUpdate(){
		return new Dictionary<UnreliableChannel, ushort>{
			{UnreliableChannel.PlayerVelocity,ushort.MaxValue},{UnreliableChannel.PlayerPosition,ushort.MaxValue},{UnreliableChannel.PlayerArrow,ushort.MaxValue},{UnreliableChannel.PlayerDirection,ushort.MaxValue},
			{UnreliableChannel.SpawnedBoxes,ushort.MaxValue}, {UnreliableChannel.SpawnedItems,ushort.MaxValue},
			{UnreliableChannel.SportBall,ushort.MaxValue},{UnreliableChannel.TrashPosition,ushort.MaxValue},{UnreliableChannel.TrashRotation,ushort.MaxValue},
			{UnreliableChannel.Payload,ushort.MaxValue}
		};
	}
	public static void ResetTransferChannelLastUpdate(){
		transferChannelLastUpdate = GetResetTransferChannelLastUpdate();
	}
	public static bool IsNewerRpc(UnreliableChannel transferChannel, ushort sentUpdate){
		ushort diff = (ushort)(sentUpdate - transferChannelLastUpdate[transferChannel]);
		// diff > 0 ignores exact duplicates. diff < 32768 ignores old/delayed packets.
		if(diff > 0 && diff < 32768){
			transferChannelLastUpdate[transferChannel] = sentUpdate;
			return true;
		}
		return false;
	}
	public static void HostIncrementLastUpdate(UnreliableChannel transferChannel){
		if(Online.IsHost()){
			transferChannelLastUpdate[transferChannel]++;
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
		return new Dictionary<ClientUnreliableChannel,ushort>(){{ClientUnreliableChannel.PlayerArrow,ushort.MaxValue}};
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
				channels.Add(id,new Dictionary<ClientUnreliableChannel,ushort>(){{ClientUnreliableChannel.PlayerArrow,ushort.MaxValue}});
			}catch{}
		}
		return channels;
	}
	public static void ResetHostClientChannelLastUpdate(){
		hostClientChannels = GetResetHostClientChannelLastUpdate();
	}
	public static bool IsNewerRpc(ClientUnreliableChannel transferChannel, int senderUUID, ushort sentUpdate){
		ushort diff = (ushort)(sentUpdate - hostClientChannels[senderUUID][transferChannel]);
		// diff > 0 ignores exact duplicates. diff < 32768 ignores old/delayed packets.
		if(diff > 0 && diff < 32768){
			hostClientChannels[senderUUID][transferChannel] = sentUpdate;
			return true;
		}
		return false;
	}
	public static void ClientIncrementUpdate(ClientUnreliableChannel transferChannel){
		clientsChannels[transferChannel]++;
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