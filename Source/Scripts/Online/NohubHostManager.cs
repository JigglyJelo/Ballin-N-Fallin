using Godot;

public partial class NohubHostManager : Node{
    public static NohubHostManager NohubManagerNode;
    private GodotObject nohubClient; 
    private GodotObject nohubConnection;
    private GDScript asyncBridgeScript;
    private string lobbyAddress;
    private string lobbyName;
    public static readonly string GAME_ID = "Ballin N Fallin" + (string)ProjectSettings.GetSetting("application/config/version");
    private string myLobbyId = ""; 

    public void Initialize(string address, string name){
        if(NohubManagerNode != null) NohubManagerNode.Free();
        NohubManagerNode = this;
        lobbyAddress = address;
        lobbyName = WordFilter.IsBadString(Online.Username) ? "Player's Game" : name;
        Name = "NohubHostManager"; 
    }

    public override void _Ready(){
        asyncBridgeScript = new GDScript();
        asyncBridgeScript.SourceCode = @"
extends RefCounted
signal task_completed(result)
func call_async(target: Object, method: String, args: Array = []):
    var result = await target.callv(method, args)
    task_completed.emit(result)
        ";
        asyncBridgeScript.Reload();

        ConnectAndRegister();
    }

    public override void _Process(double delta){
        if(nohubConnection != null && GodotObject.IsInstanceValid(nohubConnection)){
            nohubConnection.Call("poll");
        }
        if(nohubClient != null && GodotObject.IsInstanceValid(nohubClient)){
            nohubClient.Call("poll");
        }
    }

    private async void ConnectAndRegister(){
        string host = "foxssake.studio"; 
        int port = 12980;

        nohubConnection = (GodotObject)ClassDB.Instantiate("StreamPeerTCP");
        Error err = (Error)(int)nohubConnection.Call("connect_to_host",host,port);

        if(err != Error.Ok){
            GD.PrintErr("NohubHostManager: Failed to connect to matchmaking server.");
            return;
        }

        int status = (int)nohubConnection.Call("get_status");
        while(status == 1){ 
            await ToSignal(GetTree(),"process_frame");
            status = (int)nohubConnection.Call("get_status");
        }

        if(status != 2){ 
            GD.PrintErr("NohubHostManager: Connection timed out.");
            return;
        }

        GDScript clientScript = GD.Load<GDScript>("res://addons/nohub.gd/nohub_client.gd");
        nohubClient = (GodotObject)clientScript.New(nohubConnection);

        GodotObject bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"set_game",new Godot.Collections.Array{GAME_ID});
        await ToSignal(bridge,"task_completed");

        Godot.Collections.Dictionary data = new Godot.Collections.Dictionary();
        data.Add("name", lobbyName);
        data.Add("player_count", "1");
        data.Add("max_players", Game.MAX_PLAYERS.ToString());

        bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"create_lobby",new Godot.Collections.Array{lobbyAddress, data});
        Variant[] signalArgs = await ToSignal(bridge,"task_completed");
        
        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(isSuccess){
            GD.Print("NohubHostManager: Successfully registered lobby to server browser!");
            GodotObject lobbyObj = (GodotObject)result.Call("value");
            myLobbyId = (string)lobbyObj.Get("id"); 
        }else{
            GD.PrintErr("NohubHostManager: Failed to create lobby.");
        }
    }

    public static void UpdateLobbyData(){
        //Ensure the node and lobby actually exist before trying to update
        if (NohubManagerNode == null || NohubManagerNode.nohubClient == null || string.IsNullOrEmpty(NohubManagerNode.myLobbyId)) return;

        //Gather players and spectators, then subtract disconnected players
        int playerCount = 0;
        if(Game.PlayerDatas != null) playerCount += Game.PlayerDatas.Count;
        if(Game.SpectatorDatas != null) playerCount += Game.SpectatorDatas.Count;
        if(Game.DisconnectedDatas != null) playerCount -= Game.DisconnectedDatas.Count;

        Godot.Collections.Dictionary updatedData = new Godot.Collections.Dictionary();
        updatedData.Add("name", NohubManagerNode.lobbyName);
        updatedData.Add("player_count", playerCount.ToString());
        updatedData.Add("max_players", Game.MAX_PLAYERS.ToString());

        GodotObject updateBridge = (GodotObject)NohubManagerNode.asyncBridgeScript.New();
        updateBridge.Call("call_async", NohubManagerNode.nohubClient, "set_lobby_data", new Godot.Collections.Array{NohubManagerNode.myLobbyId, updatedData});
    }

    public static async void LockLobby(){
        if(NohubManagerNode.nohubClient == null || !GodotObject.IsInstanceValid(NohubManagerNode.nohubClient) || string.IsNullOrEmpty(NohubManagerNode.myLobbyId)){
            GD.PrintErr("NohubHostManager: Cannot lock lobby. Client invalid or lobby not created yet.");
            return;
        }

        GodotObject bridge = (GodotObject)NohubManagerNode.asyncBridgeScript.New();
        
        // This natively locks the lobby on the server. You don't need to do anything else!
        bridge.Call("call_async",NohubManagerNode.nohubClient,"lock_lobby",new Godot.Collections.Array{NohubManagerNode.myLobbyId});
        Variant[] signalArgs = await NohubManagerNode.ToSignal(bridge,"task_completed");

        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(isSuccess){
            GD.Print("NohubHostManager: Successfully locked the lobby.");
        }else{
            GD.PrintErr("NohubHostManager: Failed to lock the lobby.");
        }
    }

    public static async void UnlockLobby(){
        if(NohubManagerNode.nohubClient == null || !GodotObject.IsInstanceValid(NohubManagerNode.nohubClient) || string.IsNullOrEmpty(NohubManagerNode.myLobbyId)){
            GD.PrintErr("NohubHostManager: Cannot unlock lobby. Client invalid or lobby not created yet.");
            return;
        }

        GodotObject bridge = (GodotObject)NohubManagerNode.asyncBridgeScript.New();
        bridge.Call("call_async",NohubManagerNode.nohubClient,"unlock_lobby",new Godot.Collections.Array{NohubManagerNode.myLobbyId});
        Variant[] signalArgs = await NohubManagerNode.ToSignal(bridge,"task_completed");

        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(isSuccess){
            GD.Print("NohubHostManager: Successfully unlocked the lobby.");
        }else{
            GD.PrintErr("NohubHostManager: Failed to unlock the lobby.");
        }
    }

    public override void _ExitTree(){
        if(nohubClient != null && GodotObject.IsInstanceValid(nohubClient) && !string.IsNullOrEmpty(myLobbyId)){
            GodotObject bridge = (GodotObject)asyncBridgeScript.New();
            bridge.Call("call_async",nohubClient,"delete_lobby",new Godot.Collections.Array{myLobbyId});
        }

        if(nohubConnection != null && GodotObject.IsInstanceValid(nohubConnection)){
            nohubConnection.Call("disconnect_from_host");
            GD.Print("NohubHostManager: Disconnected and cleaned up lobby.");
        }
        NohubManagerNode = null;
    }
}