using Godot;

public partial class NoraySetup : Node{
    public static Node BridgeNode;

    public static void InitializeBridge(){
        if (BridgeNode == null) {
            GDScript bridgeScript = GD.Load<GDScript>("res://Source/Scripts/Online/NorayBridge.gd");
            if (bridgeScript == null) {
                GD.PrintErr("NorayBridge.gd could not be found!");
                return;
            }
            
            BridgeNode = (Node)bridgeScript.New();
            Game.GameNode.AddChild(BridgeNode);
            
            BridgeNode.Connect("host_ready", Callable.From<string>((oid) => {
                Online.NorayHostOid = oid;
                GD.Print("Hosting on Noray with OID: " + oid);
            }));
            
            BridgeNode.Connect("host_failed", Callable.From<string>((err) => {
                GD.PrintErr("Noray Host Error: " + err);
                Online.FailedToStart(new OfflineMultiplayerPeer(), Error.CantCreate);
            }));
            
            BridgeNode.Connect("client_connected", Callable.From(() => {
                GD.Print("Joined via Noray!");
            }));

            BridgeNode.Connect("client_failed", Callable.From<string>((err) => {
                GD.PrintErr("Noray Client Error: " + err);
                Online.FailedToStart(new OfflineMultiplayerPeer(), Error.CantConnect);
            }));
        }
    }

    public static bool NorayHost(){
        InitializeBridge();
        BridgeNode.Call("start_host", Game.MAX_PLAYERS);
        return true;
    }

    public static bool NorayJoin(){
        InitializeBridge();
        BridgeNode.Call("start_client", Online.NorayHostOid);
        return true;
    }
}