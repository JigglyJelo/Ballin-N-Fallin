using Godot;

public partial class ChatManager : Node{
	public static ChatManager ChatNode;
	public override void _Ready(){
		ChatNode = this;
	}

	public static void SendChat(string message){
		ChatNode.Rpc(nameof(ChatNode.SendChatRPC),message);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.Chat)]
	private void SendChatRPC(string message){
		if(Game.CurrentScene != Game.SceneType.Menu){
			InGameChat.CreateChatMessage(Online.GetRpcSender(),message);
		}
	}
}