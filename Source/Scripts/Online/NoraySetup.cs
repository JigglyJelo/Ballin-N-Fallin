using Godot;

public partial class NoraySetup : Node {
    public static Node BridgeNode;

    public static void InitializeBridge() {
        if (BridgeNode == null) {
            // *** MAKE SURE THIS PATH MATCHES WHERE YOU SAVED NorayBridge.gd ***
            GDScript bridgeScript = GD.Load<GDScript>("res://Source/Scripts/NorayBridge.gd");
            BridgeNode = (Node)bridgeScript.New();
            Game.GameNode.AddChild(BridgeNode);
            
            BridgeNode.Connect("host_ready", Callable.From<string>((oid) => {
                Online.NorayHostOid = oid;
                GD.Print("Hosting on Noray with OID: " + oid);
            }));
            
            BridgeNode.Connect("host_failed", Callable.From<string>((err) => {
                Online.FailedToStart(new OfflineMultiplayerPeer(), Error.CantCreate);
            }));
            
            BridgeNode.Connect("client_connected", Callable.From(() => {
                GD.Print("Joined via Noray!");
            }));

            BridgeNode.Connect("client_failed", Callable.From<string>((err) => {
                Online.FailedToStart(new OfflineMultiplayerPeer(), Error.CantConnect);
            }));
        }
    }

    public static bool NorayHost() {
        InitializeBridge();
        BridgeNode.Call("start_host", Game.MAX_PLAYERS);
        return true; 
    }

    public static bool NorayJoin() {
        InitializeBridge();
        BridgeNode.Call("start_client", Online.NorayHostOid);
        return true; 
    }
}