using Godot;
using System;
using System.Collections.Generic;

public partial class PingGetter : Node{
	public static int LastPing;
	private PingRetrievalMethodEnum pingRetrievalMethod = PingRetrievalMethodEnum.RPC;
	private const int PING_COUNT = 6;
	public static int[] Pings = new int[Game.MAX_PLAYERS];
	
	// Pre-allocated buffers
	private static byte[] pingsData8 = new byte[Game.MAX_PLAYERS];
	private static byte[] pingsData16 = new byte[Game.MAX_PLAYERS * 2];
	
	// The rolling buffer for median calculation
	private static Queue<int> pingHistory = new Queue<int>();
	
	private ENetConnection host;
	private ENetPacketPeer yourPacketPeer;
	private bool waitingForPing = false;
	private int disconnectTimer = 0;
	
	// Caching variables for SyncPings
	private static int cachedYourIndex = -1;
	private static int cachedUUID = -1;

	// Timer for RPC pings to prevent network flooding
	private ulong lastRpcPingTime = 0;
	private const ulong RPC_PING_INTERVAL_MS = 1000; // Sends a ping every 1 second

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
				UpdatePingHistory(0); // Host ping is always 0
				
				UpdatePings();
			}
		}
	}

	private void UpdatePings(){
		if(Online.IsHost()){
			switch(pingRetrievalMethod){
				case PingRetrievalMethodEnum.EnetRTT:
					if(host == null && Game.GameNode.Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enetPeer){
						host = enetPeer.Host;
					}
					if(host != null){
						Godot.Collections.Array<ENetPacketPeer> peers = host.GetPeers();
						for(int i = 0; i < peers.Count; i++){
							peers[i].PingInterval(16);
							Pings[i + 1] = (int)peers[i].GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime);
						}
					}
					break;
				case PingRetrievalMethodEnum.RPC:
					// Only fire RPCs periodically so we don't flood the network on every physics frame
					if(Time.GetTicksMsec() - lastRpcPingTime >= RPC_PING_INTERVAL_MS){
						lastRpcPingTime = Time.GetTicksMsec();
						ulong currentTicks = Time.GetTicksMsec();
						
						// Ask every client to echo this timestamp back
						for(int i = 0; i < Game.PlayerDatas.Count; i++){
							int uuid = Game.PlayerDatas[i].UUID;
							if(uuid != 1 && uuid != Online.GetUUID()){ // Skip the host
								RpcId(uuid, nameof(ClientEchoPing), currentTicks);
							}
						}
					}
					break;
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void ClientEchoPing(ulong timestamp){
		// The client receives the host's timestamp and immediately sends it back
		RpcId(1, nameof(HostReceiveEcho), timestamp);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void HostReceiveEcho(ulong originalTimestamp){
		if(!Online.IsHost()) return;
		
		int senderId = Multiplayer.GetRemoteSenderId();
		
		// Get the raw RTT (which includes the artificial Godot frame delay)
		int rawRtt = (int)(Time.GetTicksMsec() - originalTimestamp);
		
		// Calculate the duration of one engine tick in milliseconds
		// (e.g., 60 Ticks per second = ~16.6ms)
		int tickDurationMs = (int)(1000.0 / Engine.PhysicsTicksPerSecond);
		
		// Subtract the artificial delay. 
		// The packet waits in the buffer an average of half-a-tick on the client, 
		// and half-a-tick on the host. Together, this equals exactly 1 full tick.
		int compensatedRtt = Math.Max(0, rawRtt - tickDurationMs);
		
		// Update the host's master array with the accurate ping
		for(int i = 0; i < Game.PlayerDatas.Count; i++){
			if(Game.PlayerDatas[i].UUID == senderId){
				Pings[i] = compensatedRtt;
				break;
			}
		}

		// When a client actually sends back an echo (once a second per client).
		BroadcastSyncPings();
	}

	private void BroadcastSyncPings(){
		bool pingOver255 = false;
		
		// Write to the 8-bit buffer by default
		for(int i = 0; i < Pings.Length; i++){
			if(Pings[i] > 255){
				pingOver255 = true;
				break;
			}else{
				pingsData8[i] = (byte)Pings[i];
			}
		}
		
		// If a ping spikes over 255, switch to writing to the 16-bit buffer
		if(pingOver255){
			for(int i = 0; i < Pings.Length; i++){
				Buffer.BlockCopy(BitConverter.GetBytes((ushort)Pings[i]), 0, pingsData16, i * 2, 2);
			}
			Rpc(nameof(SyncPings), pingsData16);
		}else{
			Rpc(nameof(SyncPings), pingsData8);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncPings(byte[] pingsData){
		int currentUUID = Online.GetUUID();
		
		// Verify if the cache is still valid by checking if the UUID at our cached index is STILL us
		bool isCacheValid = 
			cachedUUID == currentUUID &&
			cachedYourIndex >= 0 &&
			cachedYourIndex < Game.PlayerDatas.Count &&
			Game.PlayerDatas[cachedYourIndex].UUID == currentUUID;

		if(!isCacheValid){
			cachedUUID = currentUUID;
			cachedYourIndex = -1;
			for(int i = 0; i < Game.PlayerDatas.Count; i++){
				if(Game.PlayerDatas[i].UUID == currentUUID){
					cachedYourIndex = i;
					break;
				}
			}
		}
		
		int yourIndex = cachedYourIndex;

		if(pingsData.Length == Pings.Length){ // 8-bit mode
			for(int i = 0; i < Pings.Length; i++){
				Pings[i] = pingsData[i];
				if(i == yourIndex){
					LastPing = Pings[i];
					UpdatePingHistory(LastPing);
				}
			}
		}else{ // 16-bit mode
			for(int i = 0; i < Pings.Length; i++){
				Pings[i] = BitConverter.ToUInt16(pingsData, i * 2);
				if(i == yourIndex){
					LastPing = Pings[i];
					UpdatePingHistory(LastPing);
				}
			}
		}
	}

	private static void UpdatePingHistory(int newPing){
		pingHistory.Enqueue(newPing);
		while(pingHistory.Count > PING_COUNT){
			pingHistory.Dequeue();
		}
	}

	public static int GetMedianPing(){
		if(pingHistory.Count == 0) return LastPing;
		
		int[] pingArray = pingHistory.ToArray();
		Array.Sort(pingArray);
		
		int middleIndex = pingArray.Length / 2;
		if(pingArray.Length % 2 == 1){
			return pingArray[middleIndex];
		}else{
			return (pingArray[middleIndex - 1] + pingArray[middleIndex]) / 2;
		}
	}

	public static int PingToTicks(int ping){
		return (int)Math.Ceiling(ping / (1000.0 / Engine.PhysicsTicksPerSecond));
	}

	public static int PingOneWayToTicks(int ping){
		return (int)Math.Ceiling(ping / (1000.0 / Engine.PhysicsTicksPerSecond) / 2.0);
	}

	public static int PingToRecommendedTicks(int ping){
		int totalTicks = PingToTicks(ping);
		
		// Baseline delay is 2 ticks. As totalTicks increases, the target delay scales up by half.
		int targetDelay = Math.Max(2, (int)Math.Ceiling(totalTicks / 2.0));
		
		// Calculate how many ticks we need to skip to achieve that target delay
		int skippedTicks = Math.Max(0, totalTicks - targetDelay);
		
		// Hard cap the maximum amount of predicted/skipped ticks to 6
		return Math.Min(6, skippedTicks);
	}

	private enum PingRetrievalMethodEnum{
		EnetRTT, RPC
	}
}