using Godot;

public partial class EnetSetup{
	public static bool EnetHost(){
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error enetError;
		int attempts = 0;
		do{
			enetError = peer.CreateServer(Online.Port,Game.MAX_PLAYERS);
		}while(enetError == Error.CantCreate && ++attempts < 127);

		if(enetError != Error.Ok){
			Online.FailedToStart(peer,enetError);
			return false;
		}else{
			Game.GameNode.Multiplayer.MultiplayerPeer = peer;
			peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
			peer.Host.ChannelLimit((int)Online.TransferChannelEnum.SendHostPing + 3);
			Game.GameNode.Multiplayer.MultiplayerPeer.RefuseNewConnections = false;
			GD.Print("Hosting");
			return true;
		}
	}

	public static bool EnetJoin(){
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error enetError = peer.CreateClient(Online.Address,Online.Port);
		if(enetError != Error.Ok){
			Online.FailedToStart(peer,enetError);
			return false;
		}else{
			Game.GameNode.Multiplayer.MultiplayerPeer = peer;
			peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
			GD.Print("Joining...");
		}
		return true;
	}
}