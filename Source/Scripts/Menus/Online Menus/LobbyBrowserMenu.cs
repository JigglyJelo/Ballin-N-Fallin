using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class LobbyBrowserMenu : ScrollableMenu{
    // Key = Lobby ID, Value = JSON-Like Dictionary Key = string data name, Value = Variant typed data
    private Dictionary<string, Dictionary<string, Variant>> lobbyIds = new Dictionary<string, Dictionary<string, Variant>>();
    private Label statusLabel, lobbyInfoLabel;
    private GodotObject nohubClient;
    private GodotObject nohubConnection;
    private const string REFRESH_KEY = "REFRESH";
    private const float X_POS = -1875;
    private const float START_Y_POS = -800;
    private float yPos = START_Y_POS;
    private GDScript asyncBridgeScript;
    private PackedScene lobbyLabelScene = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LobbyLabel.tscn");

    public override void _Ready(){
        base._Ready();
        statusLabel = GetNode<Label>("StatusLabel");
        lobbyInfoLabel = GetNode<Label>("LobbyInfoLabel");
        asyncBridgeScript = new GDScript();
        asyncBridgeScript.SourceCode = @"
extends RefCounted
signal task_completed(result)
func call_async(target: Object, method: String, args: Array = []):
    var result = await target.callv(method, args)
    task_completed.emit(result)
        ";
        asyncBridgeScript.Reload();

        UpdateStatus("Connecting to Nohub...");
        InitializeNohub();
    }

    public override void _Process(double delta){
        InputChecks(delta,(int)Online.InputId);

        if(nohubConnection != null && GodotObject.IsInstanceValid(nohubConnection)){
            nohubConnection.Call("poll");
        }
        if(nohubClient != null && GodotObject.IsInstanceValid(nohubClient)){
            nohubClient.Call("poll");
        }
    }

    private async void InitializeNohub(){
        string host = "foxssake.studio"; 
        int port = 12980;

        nohubConnection = (GodotObject)ClassDB.Instantiate("StreamPeerTCP");
        Error error = (Error)(int)nohubConnection.Call("connect_to_host",host,port);

        if(error != Error.Ok){
            UpdateStatus("Failed to connect to matchmaking server.");
            return;
        }

        int status = (int)nohubConnection.Call("get_status");
        while(status == 1){ 
            await ToSignal(GetTree(),"process_frame");
            status = (int)nohubConnection.Call("get_status");
        }

        if(status != 2){ 
            UpdateStatus("Connection timed out.");
            return;
        }

        GDScript clientScript = GD.Load<GDScript>("res://addons/nohub.gd/nohub_client.gd");
        nohubClient = (GodotObject)clientScript.New(nohubConnection);

        UpdateStatus("Setting Game ID...");
        GodotObject bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"set_game",new Godot.Collections.Array{NohubHostManager.GAME_ID});
        
        Variant[] signalArgs = await ToSignal(bridge,"task_completed");
        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(!isSuccess){
            UpdateStatus("Server rejected Game ID.");
            return;
        }

        UpdateStatus("Fetching Lobbies...");
        FetchLobbies();
    }

    private async void FetchLobbies(){
        if(nohubClient == null) return;

        GodotObject bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"list_lobbies",new Godot.Collections.Array());
        
        Variant[] signalArgs = await ToSignal(bridge,"task_completed");
        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(!isSuccess){
            UpdateStatus("Failed to fetch lobbies.");
            return;
        }

        Godot.Collections.Array lobbies = (Godot.Collections.Array)result.Call("value");

        foreach(Node child in selectionsContainer.GetChildren()){
            child.QueueFree();
        }
        selectionsContainer.GetChildren().Clear();
        lobbyIds.Clear();

        await ToSignal(GetTree(),"process_frame"); 

        int index = 0;
        yPos = START_Y_POS; 

        lobbyIds.Add(REFRESH_KEY, null);
        Label refreshLabel = lobbyLabelScene.Instantiate<Label>();
        refreshLabel.Text = "Refresh List";
        refreshLabel.Name = "Lobby" + index; 
        refreshLabel.Position = new Vector2(X_POS,yPos);
        refreshLabel.Scale = Vector2.One;
        selectionsContainer.AddChild(refreshLabel);
        yPos += 200;
        index++;

        if(lobbies.Count == 0){
            UpdateStatus("No active games found.");
        }else{
            UpdateStatus("Select a Match to Join");

            foreach(GodotObject lobby in lobbies){
                string id = (string)lobby.Get("id");
                Godot.Collections.Dictionary data = (Godot.Collections.Dictionary)lobby.Get("data");
                
                //Parse the custom Schema items
                if(!TryParseLobbyData(data, id, out Dictionary<string, Variant> lobbyData)) continue;

                bool lockStatus = (bool)lobby.Get("is_locked"); 

                lobbyData.Add("locked", lockStatus);
                lobbyIds.Add(id, lobbyData);

                string lobbyName = lobbyData["name"].AsString();
                int currentPlayers = lobbyData["player_count"].AsInt32(); 
                int maxPlayers = lobbyData["max_players"].AsInt32();
                Label lobbyLabel = lobbyLabelScene.Instantiate<Label>();
                lobbyLabel.Text = $"{lobbyName} ({currentPlayers}/{maxPlayers})";
                lobbyLabel.Name = "Lobby" + index; 
                lobbyLabel.Position = new Vector2(X_POS,yPos);
                lobbyLabel.Scale = Vector2.One;
                lobbyLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
                selectionsContainer.AddChild(lobbyLabel);
                yPos += 200;
                index++;
            }
        }

        Selections = selectionsContainer.GetChildren();
        totalSelections = Selections.Count;
        Selection = 1;
        
        UpdateSelectionVisual();
    }

    private bool TryParseLobbyData(Godot.Collections.Dictionary data, string id, out Dictionary<string, Variant> parsedData) {
        parsedData = new Dictionary<string, Variant>();
        foreach(var requirement in NohubHostManager.LOBBY_SCHEMA){
            string expectedKey = requirement.Key;
            Variant.Type expectedType = requirement.Value;
            if(!data.ContainsKey(expectedKey)) return false; 

            string rawValue = data[expectedKey].AsString();
            switch(expectedType){
                case Variant.Type.String:
                    parsedData[expectedKey] = rawValue;
                    break;
                case Variant.Type.Bool:
                    if(bool.TryParse(rawValue, out bool parsedBool)){
                        parsedData[expectedKey] = parsedBool;
                    }else{
                        GD.PrintErr($"Lobby {id} rejected: '{expectedKey}' could not be parsed to Bool.");
                        return false; 
                    }
                    break;
                case Variant.Type.Int:
                    if(int.TryParse(rawValue, out int parsedInt)){
                        parsedData[expectedKey] = parsedInt;
                    }else{
                        GD.PrintErr($"Lobby {id} rejected: '{expectedKey}' could not be parsed to Int.");
                        return false; 
                    }
                    break;
            }
        }
        return true; 
    }

    protected override void UpdateSelectionVisual() {
        base.UpdateSelectionVisual(); 

        if(Selections == null || Selections.Count == 0) return;

        for(int i = 0; i < Selections.Count; i++){
            Label label = Selections[i] as Label;
            if (label == null) continue;
            
            string key = lobbyIds.ElementAt(i).Key;
            if (key.Equals(REFRESH_KEY)) continue;

            bool isLocked = lobbyIds[key]["locked"].AsBool();
            if(Selection == i + 1){
                label.SelfModulate = isLocked ? Colors.Red : Colors.Green; 
            }else{
                label.SelfModulate = isLocked ? Colors.Gray : Colors.White; 
            }
        }
        
        lobbyInfoLabel.Text = CreateLobbyInfoString();
    }

    private string CreateLobbyInfoString(){
        string selectedLobbyId = lobbyIds.ElementAt(Selection-1).Key;
        if(selectedLobbyId.Equals(REFRESH_KEY)){
            return "Refresh lobby list";
        }else{
            Dictionary<string,Variant> lobbyData = lobbyIds[selectedLobbyId];
            StringBuilder stringBuilder = new StringBuilder(lobbyData["name"].AsString());
            stringBuilder.Append($"\nPlayers: ({lobbyData["player_count"].AsString()}/{lobbyData["max_players"].AsString()})");
            stringBuilder.Append($"\nItems: {(lobbyData["items_enabled"].AsBool() ? "On" : "Off")}");
            bool isTour = lobbyData["is_tour"].AsBool();
            if(isTour){
                stringBuilder.Append($"\nTour\nPoints to Win: {lobbyData["points_to_win"].AsInt32()}");
            }else{
                stringBuilder.Append($"\nFreeplay");
            }
            return stringBuilder.ToString();
        }
    }

    protected override async void MenuChoose(int choice){
        if(lobbyIds.Count == 0) return;

        SFX.Play("Confirm");
        int index = choice - 1; 
        
        string selectedLobbyId = lobbyIds.ElementAt(index).Key;

        if(selectedLobbyId.Equals(REFRESH_KEY)){
            UpdateStatus("Refreshing...");
            FetchLobbies();
            return;
        }

        if(lobbyIds[selectedLobbyId]["locked"].AsBool()) {
            SFX.Play("Error"); 
            UpdateStatus("That lobby is currently locked.");
            return;
        }

        int players = lobbyIds[selectedLobbyId]["player_count"].AsInt32();
        int max = lobbyIds[selectedLobbyId]["max_players"].AsInt32();

        if(players >= max){
            SFX.Play("Error");
            UpdateStatus("That lobby is currently full.");
            return;
        }

        UpdateStatus($"Joining {selectedLobbyId}...");

        GodotObject bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"join_lobby",new Godot.Collections.Array{selectedLobbyId});
        
        Variant[] signalArgs = await ToSignal(bridge,"task_completed");
        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(isSuccess){
            string address = (string)result.Call("value");
            UpdateStatus("Connected! Launching...");
            
            if(address.StartsWith("noray://")){
                Online.Network = Online.NetworkType.Noray;
                Online.NorayHostOid = address.Replace("noray://", "");
            }else if(address.StartsWith("enet://")){
                Online.Network = Online.NetworkType.Direct;
                string ipAndPort = address.Replace("enet://", "");
                string[] split = ipAndPort.Split(':');
                Online.Address = split[0];
                if(split.Length > 1) Online.Port = ushort.Parse(split[1]);
            }

            if(nohubConnection != null && GodotObject.IsInstanceValid(nohubConnection)){
                nohubConnection.Call("disconnect_from_host");
            }

            OnlineLobby lobby = GD.Load<PackedScene>(MenuScene.MENU_PATH + "Online/OnlineLobby.tscn").Instantiate<OnlineLobby>();
            lobby.IsHost = false;
            GetParent().AddChild(lobby);
            QueueFree();
        }else{
            UpdateStatus("Failed to join lobby.");
        }
    }

    public override void MenuBack(){
        SFX.Play("Back");

        if(nohubConnection != null && GodotObject.IsInstanceValid(nohubConnection)){
            nohubConnection.Call("disconnect_from_host");
        }
        MenuScene.LoadMenu("Online/OnlineMenu");
        QueueFree();
    }

    private void UpdateStatus(string message){
        if(statusLabel != null){
            statusLabel.Text = message;
        }
    }
}