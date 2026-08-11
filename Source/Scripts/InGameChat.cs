using Godot;
using System.Collections.Generic;

public partial class InGameChat : VBoxContainer{
	private static InGameChat InGameChatNode;
	private ScrollContainer scrollContainer;
	private VBoxContainer messageList;
	private LineEdit textEntry;
	private readonly static PackedScene IN_GAME_CHAT_SCENE = GD.Load<PackedScene>("res://Source/Scenes/OnlineChat.tscn");
	private readonly static PackedScene CHAT_MESSAGE_SCENE = GD.Load<PackedScene>("res://Source/Scenes/ChatMessage.tscn");
	
	private const float MESSAGE_LIFETIME = 10;
	private const float FADE_TIME = 1;
	private Dictionary<Label, float> messageTimers = new Dictionary<Label, float>();

	public override void _Ready(){
		(GetParent().GetParent() as CanvasLayer).Scale = Game.ContentScaleVector2;
		InGameChatNode = this;
		scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
		messageList = scrollContainer.GetNode<VBoxContainer>("MessageList");
		textEntry = GetNode<LineEdit>("LineEdit");
		textEntry.MaxLength = ChatManager.MESSAGE_LENGTH;
		textEntry.TextSubmitted += OnTextSubmitted;
	}

	public override void _Process(double delta){
		if(messageTimers.Count == 0) return;

		List<Label> activeLabels = new List<Label>(messageTimers.Keys);

		foreach(Label lbl in activeLabels){
			if(!IsInstanceValid(lbl)){
				messageTimers.Remove(lbl);
				continue;
			}

			messageTimers[lbl] -= (float)delta;

			if(messageTimers[lbl] <= FADE_TIME){
				Color mod = lbl.Modulate;
				mod.A = Mathf.Clamp(messageTimers[lbl] / FADE_TIME, 0f, 1f);
				lbl.Modulate = mod;
			}

			if(messageTimers[lbl] <= 0f){
				lbl.QueueFree();
				messageTimers.Remove(lbl);
			}
		}
	}

	private void OnTextSubmitted(string message){
		if(!string.IsNullOrWhiteSpace(message)){
			ChatManager.SendChat(message);
		}
		textEntry.Clear();
		textEntry.ReleaseFocus();
	}

	public override void _UnhandledInput(InputEvent @event){
		if(SettingsOnlineMenu.OnlineChatSetting != SettingsOnlineMenu.ChatSetting.Disabled){
			if(@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.IsEcho()){
				if(keyEvent.Keycode == Key.T){
					textEntry.GrabFocus();
					GetViewport().SetInputAsHandled(); 
				}
			}
		}
	}

	public static void CreateChatMessage(int senderUUID, string message){
		if(SettingsOnlineMenu.OnlineChatSetting != SettingsOnlineMenu.ChatSetting.Disabled){
			foreach(PlayerData playerData in Game.PlayerDatas){
				if(playerData.UUID == senderUUID){
					if(SettingsOnlineMenu.OnlineChatSetting == SettingsOnlineMenu.ChatSetting.Filtered){
						message = WordFilter.FilterChatMessage(message);
					}
					Label messageLabel = CHAT_MESSAGE_SCENE.Instantiate<Label>();
					messageLabel.Text = $"{playerData.Username}: {message}";
					messageLabel.SelfModulate = playerData.PlayerColor;
					InGameChatNode.AddMessageToUI(messageLabel);
					break;
				}
			}
		}
	}

	private async void AddMessageToUI(Label messageLabel){
		messageList.AddChild(messageLabel);
		messageTimers[messageLabel] = MESSAGE_LIFETIME;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		VScrollBar scrollbar = scrollContainer.GetVScrollBar();
		scrollContainer.ScrollVertical = (int)scrollbar.MaxValue;
	}

	public static void SpawnInGameChat(){
		if(InGameChatNode != null){
			InGameChatNode.QueueFree();
		}
		Game.GameNode.AddChild(IN_GAME_CHAT_SCENE.Instantiate());
	}

	public static void DeleteInGameChat(){
		if(InGameChatNode != null){
			InGameChatNode.QueueFree();
		}else if(Game.GameNode.GetNode("OnlineChat") != null){
			Game.GameNode.GetNode("OnlineChat").QueueFree();
		}
	}
}