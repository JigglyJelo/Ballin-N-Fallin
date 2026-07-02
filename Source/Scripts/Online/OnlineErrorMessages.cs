public static class OnlineErrorMessages{
    public static string NonHostCallErrorMessage(){
        return Online.GetRpcSender() + " sent this RPC to you but only the Host should be receiving this RPC";
    }
    public static string ClientSpoofErrorMessage(int uuid){
        return Online.GetRpcSender() + " sent this RPC pretending to be " + uuid;
    }
}