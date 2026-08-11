using Godot;

public partial class InGameChat : VBoxContainer{
	private static InGameChat InGameChatNode;
	private VBoxContainer messageList;
	private readonly static PackedScene CHAT_MESSAGE_SCENE = GD.Load<PackedScene>("res://Source/Scenes/ChatMessage.tscn");
	public override void _Ready(){
		InGameChatNode = this;
		messageList = GetNode<VBoxContainer>("ScrollContainer/MessageList");
	}

	public static void CreateChatMessage(int senderUUID, string message){
		foreach(PlayerData playerData in Game.PlayerDatas){
			if(playerData.UUID == senderUUID){
				Label messageLabel = CHAT_MESSAGE_SCENE.Instantiate<Label>();
				messageLabel.Text = $"{playerData.Username}: {message}";
				messageLabel.SelfModulate = playerData.PlayerColor;
				InGameChatNode.messageList.AddChild(messageLabel);
				break;
			}
		}
	}
}