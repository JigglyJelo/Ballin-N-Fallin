using Godot;
using System.Collections.Generic;

public partial class ChatManager : Node{
	public const int MESSAGE_LENGTH = 127;
	public static ChatManager ChatNode;
	private static List<int> mutedPlayers;
	
	public override void _Ready(){
		ChatNode = this;
		mutedPlayers = new List<int>();
	}

	public static void SendChat(string message){
		ChatNode.Rpc(nameof(ChatNode.SendChatRPC), message);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.Chat)]
	private void SendChatRPC(string message){
		int senderId = Online.GetRpcSender();
		
		if(mutedPlayers.Contains(senderId)) return;

		if(message.Length <= MESSAGE_LENGTH){
			InGameChat.CreateChatMessage(senderId, message);
		}
	}

	public static void ResetMutedPlayers(){
		mutedPlayers = new List<int>();
	}

	public static void MutePlayer(string username){
		List<int> playerUUIDs = Online.UsernameToUUIDs(username);
		
		if(playerUUIDs == null || playerUUIDs.Count == 0){
			InGameChat.CreateNonChatMessage(InGameChat.ERROR_COLOR, $"No player named {username} was found.");
			return;
		}

		int newlyMutedCount = 0;
		foreach(int uuid in playerUUIDs){
			if(!mutedPlayers.Contains(uuid)){
				mutedPlayers.Add(uuid);
				newlyMutedCount++;
			}
		}

		if(newlyMutedCount > 0){
			InGameChat.CreateNonChatMessage(Colors.White, $"Muted {newlyMutedCount} player(s) named {username}.");
		}else{
			InGameChat.CreateNonChatMessage(InGameChat.ERROR_COLOR, $"All players named {username} are already muted.");
		}
	}

	public static void UnmutePlayer(string username){
		List<int> playerUUIDs = Online.UsernameToUUIDs(username);
		
		if(playerUUIDs == null || playerUUIDs.Count == 0){
			InGameChat.CreateNonChatMessage(InGameChat.ERROR_COLOR, $"No player named {username} was found.");
			return;
		}

		int unmutedCount = 0;
		foreach(int uuid in playerUUIDs){
			if(mutedPlayers.Contains(uuid)){
				mutedPlayers.Remove(uuid);
				unmutedCount++;
			}
		}

		if(unmutedCount > 0){
			InGameChat.CreateNonChatMessage(Colors.White, $"Unmuted {unmutedCount} player(s) named {username}.");
		}else{
			InGameChat.CreateNonChatMessage(InGameChat.ERROR_COLOR, $"No player named {username} was found muted.");
		}
	}
}