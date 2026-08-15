using Godot;
using System;
using System.Collections.Generic;

public partial class InGameChat : VBoxContainer{
	public static InGameChat InGameChatNode = null;
	[Export]
	private bool isLobbyChat = false;
	private ScrollContainer scrollContainer;
	private VBoxContainer messageList;
	private LineEdit textEntry;
	private readonly static PackedScene IN_GAME_CHAT_SCENE = GD.Load<PackedScene>("res://Source/Scenes/OnlineChat.tscn");
	private readonly static PackedScene LOBBY_CHAT_MESSAGE_SCENE = GD.Load<PackedScene>("res://Source/Scenes/LobbyChatMessage.tscn");
	private readonly static PackedScene IN_GAME_CHAT_MESSAGE_SCENE = GD.Load<PackedScene>("res://Source/Scenes/ChatMessage.tscn");
	
	private const float MESSAGE_LIFETIME = 10;
	private const float FADE_TIME = 1;
	private Dictionary<Label, float> messageTimers = new Dictionary<Label, float>();
	public static readonly Color ERROR_COLOR = Colors.Red;

	public override void _Ready(){
		if(InGameChatNode != null && !InGameChatNode.IsQueuedForDeletion()){
			InGameChatNode.QueueFree();
		}
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
			if(message.StartsWith("/")){
				RunCommand(message);
			}else{
				ChatManager.SendChat(message);
			}
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

	private void RunCommand(string message){
		string[] parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if(parts.Length == 0) return;

		string command = parts[0].ToLower();
		string argument = "";

		if(parts.Length > 1){
			argument = string.Join(" ", parts, 1, parts.Length - 1);
		}

		switch(command){
			case "/spectate":
				if(isLobbyChat && GetParent().GetParent().GetParent() is LobbySettingsMenu lobbySettingsMenu){
					lobbySettingsMenu.EnterSpectatorMode();
				}else{
					CreateNonChatMessage(ERROR_COLOR, "Can't enter spectator mode currently.");
				}
				break;
			case "/kick":
				if(Online.IsHost()){
					if(argument != ""){
						bool kickedPlayer = false;
						foreach(PlayerData player in Game.PlayerDatas){
							if(player.Username == argument){
								Online.KickPlayer(player.UUID);
								kickedPlayer = true;
							}
						}
						if(!kickedPlayer) CreateNonChatMessage(ERROR_COLOR, $"No player named {argument} in the lobby.");
					}else{
						CreateNonChatMessage(ERROR_COLOR, "Usage: /kick <playername>");
					}
				}else{
					CreateNonChatMessage(ERROR_COLOR, "Only the host can kick players.");
				}
				break;
			case "/ban":
				if(Online.IsHost()){
					if(argument != ""){
						bool bannedPlayer = false;
						foreach(PlayerData player in Game.PlayerDatas){
							if(player.Username == argument){
								Online.BanPlayer(player.UUID);
								bannedPlayer = true;
							}
						}
						if(!bannedPlayer) CreateNonChatMessage(ERROR_COLOR, $"No player named {argument} in the lobby.");
					}else{
						CreateNonChatMessage(ERROR_COLOR, "Usage: /ban <playername>");
					}
				}else{
					CreateNonChatMessage(ERROR_COLOR, "Only the host can ban players.");
				}
				break;
			case "/mute":
				if(argument != ""){
					ChatManager.MutePlayer(argument);
				}else{
					CreateNonChatMessage(ERROR_COLOR, "Usage: /mute <playername>");
				}
				break;
			case "/unmute":
				if(argument != ""){
					ChatManager.UnmutePlayer(argument);
				}else{
					CreateNonChatMessage(ERROR_COLOR, "Usage: /unmute <playername>");
				}
				break;
			case "/help":
				CreateNonChatMessage(Colors.White, @"Command List:
				/spectate - Enter spectator mode.
				/mute <player> - Hide all messages from a player.
				/unmute <player> - Unmute a player.
				Host Commands:
				/ban <player> - Ban a player from the lobby.
				/kick <player> - Kick a player from the lobby.");
				break;
			default:
				CreateNonChatMessage(ERROR_COLOR, "Unknown command. Type /help for list of commands.");
				break;
		}
	}

	public static void CreateNonChatMessage(Color messageColor, string message){
		Label label = (InGameChatNode.isLobbyChat ? LOBBY_CHAT_MESSAGE_SCENE : IN_GAME_CHAT_MESSAGE_SCENE).Instantiate<Label>();
		label.Text = message;
		label.SelfModulate = messageColor;
		InGameChatNode.AddMessageToUI(label);
	}

	public static void CreateChatMessage(int senderUUID, string message){
		if(SettingsOnlineMenu.OnlineChatSetting != SettingsOnlineMenu.ChatSetting.Disabled){
			foreach(PlayerData playerData in Game.PlayerDatas){
				if(playerData.UUID == senderUUID){
					if(SettingsOnlineMenu.OnlineChatSetting == SettingsOnlineMenu.ChatSetting.Filtered){
						message = WordFilter.FilterChatMessage(message);
					}
					Label messageLabel = (InGameChatNode.isLobbyChat ? LOBBY_CHAT_MESSAGE_SCENE : IN_GAME_CHAT_MESSAGE_SCENE).Instantiate<Label>();
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
			if(!InGameChatNode.IsQueuedForDeletion()){
				InGameChatNode.QueueFree();
				InGameChatNode = null;
			}
		}
		Game.GameNode.AddChild(IN_GAME_CHAT_SCENE.Instantiate());
	}

	public static void DeleteInGameChat(){
		if(InGameChatNode != null){
			if(!InGameChatNode.IsQueuedForDeletion()){
				InGameChatNode.QueueFree();
			}
		}else if(Game.GameNode.GetNodeOrNull("OnlineChat") != null && !Game.GameNode.GetNodeOrNull("OnlineChat").IsQueuedForDeletion()){
			Game.GameNode.GetNode("OnlineChat").QueueFree();
		}
		InGameChatNode = null;
	}
}