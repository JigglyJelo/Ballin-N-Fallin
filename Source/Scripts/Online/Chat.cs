using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class Chat : Node{
	private const int MESSAGE_HISTORY_SIZE = 16;
	private const int MAX_MESSAGE_LENGTH = 64;
	private Queue<string> chatMessages;
	public static Chat ChatNode;
	public override void _Ready(){
		ChatNode?.QueueFree();
		ChatNode = this;
		chatMessages = new Queue<string>();
	}

	public override void _Process(double delta){
	}

	public static void SendChat(string message){
		//Ensure its within length
		message = message.Substring(0, Math.Min(message.Length, MAX_MESSAGE_LENGTH));
		byte[] messageData = Encoding.UTF8.GetBytes(message);
		ChatNode.RpcId(1,nameof(SendHostChatRpc),messageData);
	}
	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.Chat)]
	private void SendHostChatRpc(byte[] messageData){
		if(Online.IsHost()){
			int senderUUID = Online.GetUUID();

		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.Chat)]
	private void SendClientsChatRpc(byte[] messageData){
		
	}
}