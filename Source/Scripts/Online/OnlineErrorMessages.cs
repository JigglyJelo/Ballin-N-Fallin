public class OnlineErrorMessages{
    public static string NonHostCallErrorMessage(){
        return Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId() + " sent this RPC to you but only the Host should be receiving this RPC";
    }
    public static string ClientSpoofErrorMessage(int uuid){
        return Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId() + " sent this RPC pretending to be " + uuid;
    }
}