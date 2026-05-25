using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class PingGetter : Node{
	public static int LastPing;
	private const int PING_COUNT = 6;
	public static int[] Pings = new int[Game.MAX_PLAYERS];
	private static byte[] pingsData = new byte[Game.MAX_PLAYERS*2];
	private static Queue<int> yourPings = new Queue<int>();
	private bool waitingForPing = false;
	public static GodotThread PingThread;
	public static PingGetter pingNode;
	private MultiplayerApi multiplayer;
	private ENetMultiplayerPeer thisPacketPeer;
	private ENetConnection host;
	private static bool runPingThread;
	private Stopwatch pingStopwatch;

    public override void _Ready(){
		pingNode = this;
		if(Online.Network == Online.NetworkType.Direct){
			multiplayer = GetTree().GetMultiplayer();
			//Create ping thread
			PingThread = new GodotThread();
			if(Online.IsHost()){
				PingThread.Start(new Callable(this,nameof(ServerPingThread)),GodotThread.Priority.High);
			}else{
				PingThread.Start(new Callable(this,nameof(ClientPingThread)),GodotThread.Priority.High);
			}
		}else{
			pingStopwatch = new Stopwatch();
		}
	}
    
    public override void _Process(double delta){
		if(Online.Network == Online.NetworkType.Steam){
			if(!waitingForPing && Game.PlayerDatas.Count > 0 && !Online.IsHost()){
				RpcId(1,nameof(PingHost));
				pingStopwatch.Start();
				waitingForPing = true;
			}
		}
	}
	

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
				//LastPing = 0;
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
			}else{
				RpcId(1,nameof(SendHostPingValue),LastPing);
			}
		}
		//foreach(ENetPacketPeer peer in host.GetPeers()) GD.Print(peer.GetStatistic(ENetPacketPeer.PeerStatistic.LastRoundTripTime));
    }

	private void ServerPingThread(){
		runPingThread = true;
		List<PacketPeerUdp> peers  = new List<PacketPeerUdp>();
		List<PacketPeerUdp> peersToDisconnect = new List<PacketPeerUdp>();
		while(runPingThread){
			Online.PingServer.Poll();
        	if(Online.PingServer.IsConnectionAvailable()){
        	    PacketPeerUdp peer = Online.PingServer.TakeConnection();
        	    peers.Add(peer);
        	}
			
        	foreach(PacketPeerUdp peer in peers){
				if(peer.GetAvailablePacketCount() > 0){
					peer.PutPacket(peer.GetPacket());
					if(peer.GetPacketError() == Error.InvalidParameter) peersToDisconnect.Add(peer);
				}
        	}
			foreach(PacketPeerUdp peer in peersToDisconnect){
				peer.Close();
				peersToDisconnect.Remove(peer);
			}
		}
	}
	private void ClientPingThread(){
		const int PING_TIMEOUT = 1000;
		runPingThread = true;
		ulong lastPingTime = Time.GetTicksMsec();
		Online.PingClient.PutPacket(BitConverter.GetBytes(Time.GetTicksMsec()));
		while(runPingThread){
			if(Online.PingClient.GetAvailablePacketCount() > 0){
				ulong ms = Time.GetTicksMsec();
				byte[] receivedPacket = Online.PingClient.GetPacket();
				ulong packetTime = BitConverter.ToUInt64(receivedPacket);
				//Only accept recent packets and ignore outdated packets
				if(packetTime >= lastPingTime){
					LastPing = (int)(ms - packetTime);
					yourPings.Enqueue(LastPing);
					if(yourPings.Count > PING_COUNT) yourPings.Dequeue();
					//Only send a new ping if it is less than ping timeout to avoid sending an extra packet that was already sent in the else if below
					if(LastPing < PING_TIMEOUT){
						Online.PingClient.PutPacket(BitConverter.GetBytes(Time.GetTicksMsec()));
						lastPingTime = Time.GetTicksMsec();
					}
				}
			}else if(Time.GetTicksMsec() - lastPingTime >= PING_TIMEOUT){
				Online.PingClient.PutPacket(BitConverter.GetBytes(Time.GetTicksMsec()));
				lastPingTime = Time.GetTicksMsec(); //Maybe remove this to include dropped packet time?
			}
		}
	}

	
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.PingGetter)]
	private void PingHost(){
		if(Online.IsHost()) RpcId(GetTree().GetMultiplayer().GetRemoteSenderId(),nameof(HostReturnPing));
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.PingGetter)]
	private void HostReturnPing(){
		pingStopwatch.Stop();
		LastPing = (int)pingStopwatch.Elapsed.TotalMilliseconds;
		yourPings.Enqueue(LastPing);
		if(yourPings.Count > PING_COUNT) yourPings.Dequeue();
		pingStopwatch.Reset();
		waitingForPing = false;
		RpcId(1,nameof(SendHostPingValue),LastPing);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.SendHostPing)]
	private void SendHostPingValue(int ping){
		if(Online.IsHost()){
			int senderId = GetTree().GetMultiplayer().GetRemoteSenderId();
			for(int i = 0; i < Game.PlayerDatas.Count; i++){
				if(Game.PlayerDatas[i].UUID == senderId){
					Pings[i] = ping;
					break;
				}
			}
		}else{
			GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
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
	}

	public static void StopPingThread(){
		runPingThread = false;
		if(PingThread != null){
			PingThread.WaitToFinish();
			GD.Print("Thread stopped");
			PingThread.Dispose();
			PingThread = null;
		}
	}

	public static int PingToTicks(int ping){
		return (int)Math.Ceiling(ping / (1000.0/Engine.PhysicsTicksPerSecond));
	}
	public static int PingOneWayToTicks(int ping){
		return (int)Math.Ceiling(ping / (1000.0/Engine.PhysicsTicksPerSecond)/2);
	}
}