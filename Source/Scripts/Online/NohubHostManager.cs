using Godot;

public partial class NohubHostManager : Node{
    private GodotObject nohubClient; 
    private GodotObject nohubConnection;
    private GDScript asyncBridgeScript;
    private string lobbyAddress;
    private string lobbyName;
    public const string GAME_ID = "BALL";
    private string myLobbyId = ""; // Track the lobby ID for cleanup

    public void Initialize(string address, string name){
        lobbyAddress = address;
        lobbyName = name;
        Name = "NohubHostManager"; // Helps us find/delete it later
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

        // 1. Set the Session Game ID
        GodotObject bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"set_game",new Godot.Collections.Array{GAME_ID});
        await ToSignal(bridge,"task_completed");

        // 2. Create the Lobby
        Godot.Collections.Dictionary data = new Godot.Collections.Dictionary();
        data.Add("name", lobbyName);

        bridge = (GodotObject)asyncBridgeScript.New();
        bridge.Call("call_async",nohubClient,"create_lobby",new Godot.Collections.Array{lobbyAddress, data});
        Variant[] signalArgs = await ToSignal(bridge,"task_completed");
        
        GodotObject result = signalArgs[0].As<GodotObject>();
        bool isSuccess = (bool)result.Call("is_success");

        if(isSuccess){
            GD.Print("NohubHostManager: Successfully registered lobby to server browser!");
            GodotObject lobbyObj = (GodotObject)result.Call("value");
            myLobbyId = (string)lobbyObj.Get("id"); // Save the ID
        }else{
            GD.PrintErr("NohubHostManager: Failed to create lobby.");
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
    }
}