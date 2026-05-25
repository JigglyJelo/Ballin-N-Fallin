using Godot;
using System.Collections.Generic;

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
			Online.PingServer = new UdpServer();
			Error udpError = Online.PingServer.Listen((ushort)(Online.Port+1));
			if(udpError != Error.Ok){
				Online.PingServer.Stop();
				Online.FailedToStart(peer,udpError);
				return false;
			}else{
				return true;
			}
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
		Online.PingClient = new PacketPeerUdp();
		Error pingError = Online.PingClient.ConnectToHost(Online.Address,Online.Port+1);
		if(pingError != Error.Ok){
			Online.FailedToStart(peer,pingError);
			return false;
		}
		return true;
	}
}