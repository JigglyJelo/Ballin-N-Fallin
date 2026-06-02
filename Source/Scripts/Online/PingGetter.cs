using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class PingGetter : Node{
	public static int LastPing;
	private const int PING_COUNT = 6;
	public static int[] Pings = new int[Game.MAX_PLAYERS];
	private static byte[] pingsData = new byte[Game.MAX_PLAYERS*2];
	//private static Queue<int> yourPings = new Queue<int>();
	private ENetConnection host;
	private bool waitingForPing = false;

    //Disconnection thing
    private int disconnectTimer = 0;

    public override void _PhysicsProcess(double delta){
        if(Online.IsOnline){
            if(Game.GameNode.Multiplayer.MultiplayerPeer != null && Game.GameNode.Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer){
                if(Online.HasDisconnected()){
					Online.Disconnect();
					SetPhysicsProcess(false);
					return;
				}
            }
        }
		if(Online.PeerIsActive() && Online.IsOnlinePeer() && !Online.HasDisconnected()){
			if(Online.IsHost()){
				LastPing = 0;
				UpdatePings();
				bool pingOver255 = false;
				if(pingsData.Length != Pings.Length) pingsData = new byte[Pings.Length];
				for(int i = 0; i < Pings.Length; i++){
					if(Pings[i] > 255){
						pingOver255 = true;
						break;
					}else{
						pingsData[i] = (byte)Pings[i];
					}
				}
				if(pingOver255){
					if(pingsData.Length != Pings.Length*2) pingsData = new byte[Pings.Length*2];
					for(int i = 0; i < Pings.Length; i++){
						Buffer.BlockCopy(BitConverter.GetBytes((ushort)Pings[i]), 0, pingsData, i*2, 2);
					}
				}
				Rpc(nameof(SyncPings),pingsData);
			}
		}
    }

	private void UpdatePings(){
		if(Online.IsHost()){
			if(host == null && Game.GameNode.Multiplayer.MultiplayerPeer is ENetMultiplayerPeer){
				host = (Game.GameNode.Multiplayer.MultiplayerPeer as ENetMultiplayerPeer).Host;
			}
			Godot.Collections.Array<ENetPacketPeer> peers = host.GetPeers();
			for(int i = 0; i < peers.Count; i++){
				Pings[i+1] = (int)peers[i].GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime); //LastRoundTripTime
				//GD.Print(Pings[i+1]);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncPings(byte[] pingsData){
		if(pingsData.Length == Pings.Length){
			for(int i = 0; i < Pings.Length; i++){
				Pings[i] = pingsData[i];
			}
		}else{
			for(int i = 0; i < Pings.Length; i++){
				Pings[i] = BitConverter.ToUInt16(pingsData, i * 2);
			}
		}
	}

	public static int GetMedianPing(){
		/*
    	int[] pingArray = yourPings.ToArray();
    	Array.Sort(pingArray);
		GD.Print(string.Join(",",pingArray));
    	int middleIndex = yourPings.Count / 2;
    	if(pingArray.Length % 2 == 1) {
        	return pingArray[middleIndex];
    	}else if(pingArray.Length == 0){
			return 0;
		}else{
			return (pingArray[middleIndex - 1] + pingArray[middleIndex]) / 2;
		}
		*/
		return 0;
	}

	public static int PingToTicks(int ping){
		return (int)Math.Ceiling(ping / (1000.0/Engine.PhysicsTicksPerSecond));
	}
	public static int PingOneWayToTicks(int ping){
		return (int)Math.Ceiling(ping / (1000.0/Engine.PhysicsTicksPerSecond)/2);
	}
}