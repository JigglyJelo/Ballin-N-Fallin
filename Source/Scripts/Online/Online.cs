using Godot;
using System.Collections.Generic;

public partial class Online{
	public static bool IsOnline = false;
	public static NetworkType Network = NetworkType.Direct;
	public static string username = "Player";
	public static string HostUsername;
	public const int USERNAME_LENGTH = 15;
	public const ushort DEFAULT_PORT = 8411;
	public static string Username{
		get{return username;}
		set{
			if(value.Length <= USERNAME_LENGTH) username = value;
			else username = value.Substring(0,USERNAME_LENGTH);
		}
	}
	public static PlayerData.PlayerInputDevice InputId = PlayerData.PlayerInputDevice.Gamepad1;
	public static ushort Port = DEFAULT_PORT;
	public static string Address = "127.0.0.1";
	public static List<string> BannedIps;
	private static float buffer = 1;
	public static float Buffer{
		get{return buffer;}
		set{
			if(value <= 0) buffer = 0;
			else if(value <= 0.5f) buffer = 0.5f;
			else if(value >= 1) buffer = 1;
			else buffer = value;
		}
	}
	public static UdpServer PingServer;
	public static PacketPeerUdp PingClient;

	public static void PredictPosition(RigidBody2D rb, byte physicsTicks){
		PredictPosition(rb,physicsTicks,null);
	}
	public static void PredictPosition(RigidBody2D rb, byte physicsTicks,Vector2? hostOriginalPosition){
		if(physicsTicks == 0) return;
		bool playerPrediction = rb.IsInGroup("Player");
		Vector2[] trailPointsToSend = new Vector2[physicsTicks > 0 ? physicsTicks - 1 : 0];
		List<Area2D> areasInScene = new List<Area2D>();
		getArea2DNodes(Game.GameNode);
		List<Area2D> areasEntered = new List<Area2D>();
		if(hostOriginalPosition != null && playerPrediction){
			Vector2 newPosition = rb.GlobalPosition;
			rb.GlobalPosition = (Vector2)hostOriginalPosition;
			getPlayerAreaOverlaps(true);
			rb.GlobalPosition = newPosition;
			getPlayerAreaOverlaps(false);
		}else{
			getPlayerAreaOverlaps(true);
		}
		
		PhysicsBody2D lastCollision = null;
		GD.Print("New prediction of " + physicsTicks + " ticks");
		if(rb.Sleeping) rb.Sleeping = false;
		if(rb.Freeze) rb.Freeze = false;
		float fDelta = 1f / Engine.PhysicsTicksPerSecond;
		Vector2 currentVelocity = rb.LinearVelocity;
		float angularVelocity = rb.AngularVelocity;
		bool stopPrediction = false;
		for(int i = 0; i < physicsTicks && !stopPrediction; i++){
			//Do necessary changes to velocities and rotation
			currentVelocity *= (float)Mathf.Clamp(1-(rb.LinearDamp/Engine.PhysicsTicksPerSecond),0,1);// Apply Linear damping  1.0-rb.LinearDamp*delta
			angularVelocity *= (float)Mathf.Clamp(1-(rb.AngularDamp/Engine.PhysicsTicksPerSecond),0,1);// Apply Angular damping 1.0-rb.AngularDamp*delta
			rb.Rotation += angularVelocity * fDelta; //Rotate
			//Gravity
			//bool airborn = rb.LinearVelocity.Y >= 0.01f && (rb.LinearVelocity.X >= 20 || rb.LinearVelocity.X <= -20); //  rb.LinearVelocity.Y <= -20 &&
			//(Vector2)ProjectSettings.GetSetting("physics/2d/default_gravity_vector") * (float)((float)ProjectSettings.GetSetting("physics/2d/default_gravity")
			//if(airborn) currentVelocity += new Vector2(0,980 * fDelta * rb.GravityScale); //Apply Gravity
			currentVelocity += new Vector2(0,980 * fDelta * rb.GravityScale);
			//Do physics tick
			KinematicCollision2D collision = rb.MoveAndCollide(currentVelocity * fDelta);
			//Check for phyisics body collisions
			if(collision != null){
				GodotObject collidedObject = collision.GetCollider();
				GD.Print((collidedObject as Node).Name);
				currentVelocity = currentVelocity.Bounce(collision.GetNormal()) * rb.PhysicsMaterialOverride.Bounce; //Apply Collision and bounce predicted rb
				if(collidedObject is RigidBody2D collidedRb){
					Vector2 newVelocity = currentVelocity.Bounce(collision.GetNormal()) * collidedRb.PhysicsMaterialOverride.Bounce; //Bounce object that collided with predicted rb
					collidedRb.LinearVelocity = newVelocity;
				}
				
				//Emit signals for Physics collisions
				if(collidedObject is PhysicsBody2D collidedPhysicsBody){
					//Create bounce particle effects at simulated position (Needed so particles spawn at collision position rather than players end precition position and to preprocess them)
					if(playerPrediction){
						Player predictedPlayer = rb.GetParent() as Player;
						Node collidedNode = collidedObject as Node;
						predictedPlayer.BounceTimer += fDelta;
						if(collidedNode.IsInGroup("Regain") || collidedNode.IsInGroup("NoRegain")){
							foreach(Player player in Game.Players){
								if(player.OwnerId != predictedPlayer.OwnerId){
									predictedPlayer.RpcId(player.OwnerId,nameof(predictedPlayer.BounceEffects),rb.GlobalPosition,currentVelocity,physicsTicks-i);
								}
							}
						}else{
							Node parent = collidedNode.GetParentOrNull<Node>();
							if(parent != null){
								if(parent.IsInGroup("Regain") || parent.IsInGroup("NoRegain")){
									foreach(Player player in Game.Players){
										if(player.OwnerId != predictedPlayer.OwnerId){
											predictedPlayer.RpcId(player.OwnerId,nameof(predictedPlayer.BounceEffects),rb.GlobalPosition,currentVelocity,physicsTicks-i);
										}
									}
								}
							}
						}
					}

					//If the collision last tick is not the same as the current collision emit signals
					if(lastCollision != collidedPhysicsBody){
						//If there was a collision last tick emit exit signals on it's object and rb
						if(lastCollision != null){
							if(lastCollision.HasSignal("body_exited")) lastCollision.EmitSignal("body_exited",rb);
							rb.EmitSignal("body_exited",lastCollision);
						}
						//Emit enter signals on the collision object occuring this tick and rb
						if(collidedPhysicsBody.HasSignal("body_entered")) collidedPhysicsBody.EmitSignal("body_entered",rb);
						rb.EmitSignal("body_entered",collidedPhysicsBody);
						//Save this collision object for next tick
						lastCollision = collidedPhysicsBody;
					}
				}
			//If there is no collision this tick but there was last tick emit exit signals on last collision
			}else if(lastCollision != null){
				if(lastCollision.HasSignal("body_exited")) lastCollision.EmitSignal("body_exited",rb);
				rb.EmitSignal("body_exited",lastCollision);
				//No collision this tick so save that by setting to null
				lastCollision = null;
			}
			
			if(playerPrediction){
				Player player = rb.GetParent() as Player;
				//Check for area collisions
				getPlayerAreaOverlaps(false);
				//Update player's trail and add point to previous positions
				if(i != physicsTicks-1){ //Skip the last point because that should be done on the clients end when they receive the player's position? Test this theory!!!
					trailPointsToSend[i] = rb.GlobalPosition;
					player.Trail.AddPoint(rb.GlobalPosition);
				}
			}
		}
		
		rb.AngularVelocity = angularVelocity;
		rb.LinearVelocity = currentVelocity;
		//Sync Trail points for clients
		if(playerPrediction){
			Player predictedPlayer = rb.GetParent() as Player;
			for(int i = 0; i < Game.PlayerDatas.Count; i++){
				int uuid = Game.PlayerDatas[i].UUID;
				Player player = Game.Players[i];
				//Send prediction to Non-Server & Non-Player Owner Clients & Don't Try to send to a disconnected Player
				if(uuid != predictedPlayer.OwnerId && player.OwnerId != 1 && !Game.DisconnectedDatas.Contains(Game.PlayerDatas[i])){
					predictedPlayer.Trail.RpcId(uuid,nameof(predictedPlayer.Trail.SyncTrail),trailPointsToSend);
				}
			}
		}
		//rb.ResetPhysicsInterpolation();
		//Local functions
		void getPlayerAreaOverlaps(bool firstIteration){
			Player player = rb.GetParent() as Player;
			foreach(Area2D area in areasInScene){
				foreach(Node node in area.GetChildren()){
					if(node is CollisionShape2D collisionShape){
						//If Player is just now entering an area this physics tick
						if(!areasEntered.Contains(area) && playerInsideArea(player,collisionShape)){
							if(!firstIteration){ //Do not emit signals on first iteration it is only meant to see whether player is initially inside an area
								if(area.IsInGroup("Stop Prediction")){
									stopPrediction = true;
								}
								area.EmitSignal("body_entered",rb);
							}
							areasEntered.Add(area);
							GD.Print("Entered " + area.Name);
						//If Player is just now leaving an area it was in the previous physics tick
						}else if(areasEntered.Contains(area) && !playerInsideArea(player,collisionShape)){
							area.EmitSignal("body_exited",rb);
							areasEntered.Remove(area);
							GD.Print("Exited " + area.Name);
						}
						break; //Already found the collision shape no need to search through area's other nodes
					}
				}
			}
		}
		//Checks if the player is inside a collisionshape
		//Only supports checking if the player (Because it is a circle) is inside a rectangular or circular shape
		bool playerInsideArea(Player player,CollisionShape2D collisionShape){
			float playerRadius = Player.RADIUS * player.PlayerScale;
			// Ensure the collisionShape is a RectangleShape2D
			switch(collisionShape.Shape){
				case RectangleShape2D rectangleShape:{
					// Get the rectangle's global transformation matrix
   					Transform2D rectTransform = collisionShape.GlobalTransform;
					Vector2 playerPosition;
					Vector2 rectSize = rectangleShape.Size;
					Vector2 rectTopLeft;
    				// Check if the rectangle is rotated
    				if(Mathf.IsZeroApprox(rectTransform.Rotation)){ // Rectangle is not rotated, perform simpler axis-aligned collision detection
        				Vector2 rectCenter = collisionShape.GlobalPosition;
        				rectTopLeft = rectCenter - (rectSize / 2);
						playerPosition = player.Rb.GlobalPosition;
    				}else{ // Rectangle is rotated, perform full transformation and collision detection
						// Get the inverse of the rectangle's transformation matrix
						// This will be used to convert global positions to the rectangle's local space
        				Transform2D rectInverseTransform = rectTransform.AffineInverse();
						// Convert the player's global position to the rectangle's local position
        				playerPosition = rectInverseTransform.BasisXform(player.Rb.GlobalPosition);
						// Calculate the top-left corner of the rectangle in local space
        				rectTopLeft = -rectSize / 2;
    				}
					// Find the closest point to the circle within the rectangle in local space
        			float closestX = Mathf.Clamp(playerPosition.X, rectTopLeft.X, rectTopLeft.X + rectSize.X);
        			float closestY = Mathf.Clamp(playerPosition.Y, rectTopLeft.Y, rectTopLeft.Y + rectSize.Y);
					// Calculate the distance between the circle's center and this closest point in local space
        			float distanceX = playerPosition.X - closestX;
        			float distanceY = playerPosition.Y - closestY;
					// Calculate the distance squared and compare with the radius squared
					float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
					return distanceSquared <= (playerRadius * playerRadius);
				}
				case CircleShape2D circleShape:{
					// Get the center position of the circle shape
    				Vector2 circleCenter = collisionShape.GlobalPosition;
    				float circleRadius = circleShape.Radius;
    				// Calculate the distance between the two circles' centers
    				float distanceX = player.Rb.GlobalPosition.X - circleCenter.X;
    				float distanceY = player.Rb.GlobalPosition.Y - circleCenter.Y;
    				float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
    				// Compare the distance squared with the sum of the radii squared
    				float combinedRadius = playerRadius + circleRadius;
    				return distanceSquared <= (combinedRadius * combinedRadius);
				}
			}
			return false;
		}
		void getArea2DNodes(Node currentNode){
        	if(currentNode is Area2D) areasInScene.Add(currentNode as Area2D);
        	foreach(Node child in currentNode.GetChildren()) getArea2DNodes(child);
    	}
	}

	public static bool IsHost(){
		if(!IsOnlinePeer()) return true;
		else if(PeerIsActive()) return Game.GameNode.Multiplayer.GetUniqueId() == 1;
		else return false;
	}

	public static bool IsRpcFromHost(){
		int id = Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId();
		if(id == 0) GD.PrintErr("IsRpcFromHost() called in Non-Rpc method");
		return id == 1;
	}

	public static bool IsConnected(){
		if(Game.GameNode.Multiplayer.MultiplayerPeer == null) return false;
		return Game.GameNode.Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
	}
	public static bool HasDisconnected(){
		if(Game.GameNode.Multiplayer.MultiplayerPeer == null) return true;
		return Game.GameNode.Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected;
	}

	public static void Disconnect(string reason){
		Node pingGetter = Game.GameNode.GetNodeOrNull("PingGetter");
		if(pingGetter != null) pingGetter.QueueFree();
		MenuScene.MenuToLoad = "Online/OnlineMenu";
		IsOnline = false;

		//Stop Ping Thread
		PingGetter.StopPingThread();
		//Disconnect UDP Ping Server
		if(PeerIsActive() && IsHost()){
			if(PingServer != null){
				PingServer.Stop();
				PingServer = null;
			}
		}else{
			if(PingClient != null){
				PingClient.Close();
				PingClient = null;
			}
		}

		//Host shut down Enet server

		if(Game.GameNode.Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected){
			if(Game.GameNode.Multiplayer.MultiplayerPeer != null){
				switch(Game.GameNode.Multiplayer.MultiplayerPeer){
					case ENetMultiplayerPeer:
						ENetMultiplayerPeer peerToDispose = Game.GameNode.Multiplayer.MultiplayerPeer as ENetMultiplayerPeer;
						Game.GameNode.Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
						ENetConnection host = peerToDispose.Host;
						if(peerToDispose != null){
							peerToDispose.Dispose();
							peerToDispose = null;
						}
						if(host != null){
							host.Destroy();
						}
						break;
					case OfflineMultiplayerPeer: break;
					default: //SteamMultiplayerPeer
						//Steam.SteamManagerNode.Call("leave_steam_lobby");
						//Game.GameNode.Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
						break;
				}
			}
		}
		Game.GameNode.Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
		//Game.GameNode.Multiplayer.MultiplayerPeer = null;

		Game.PlayerDatas = new List<PlayerData>();
		GD.Print(reason);
		Game.GameNode.GetTree().Paused = false;
		Engine.TimeScale = 1;
		Game.Paused = false;
		PingGetter.LastPing = 0;
		SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
	}

	public static void Disconnect(){
		Disconnect("Disconnected");
	}

	public static void PlayerDisconnected(long id){
		if(IsHost()){
			bool removePlayer = Game.Players == null || Game.Players.Length == 0;
			Game.TellClientsWhatToDoAboutDisconnectedPlayer(removePlayer, (int)id);
		}
	}


	public static void RemoveDisconnectedPlayerInfos(){
		if(Online.IsOnline && Game.DisconnectedDatas.Count > 0){
			foreach(PlayerData playerInfo in Game.DisconnectedDatas){
				int index = Game.PlayerDatas.IndexOf(playerInfo);
				Game.PlayerDatas.Remove(playerInfo);
				List<int> scores = new List<int>();
				for(int i = 0; i < Tour.PlayerScores.Length; i++){
					scores.Add(Tour.PlayerScores[i]);
				}
				if(index != -1) scores.RemoveAt(index);
				for(int i = 0; i < scores.Count; i++){
					Tour.PlayerScores[i] = scores[i];
				}
			}
			Game.DisconnectedDatas = new List<PlayerData>();
			Game.TotalPlayers = (byte)Game.PlayerDatas.Count;
			if(Game.CurrentScene == Game.SceneType.Game && Online.IsHost()) Game.GameNode.GetNode<PlayerSync>("Scene/PlayerSynchronizer").ResetSyncArrayLengths();
		}
	}

	public static bool PeerIsActive(){
		return Game.GameNode.Multiplayer.MultiplayerPeer!= null && Game.GameNode.Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && IsOnlinePeer();
	}

	public static void KickPlayer(int uuid){
		if(IsHost() && uuid != 1){
			(Game.GameNode.Multiplayer as SceneMultiplayer).DisconnectPeer(uuid);
			PlayerDisconnected(uuid);
		}
	}

	public static void BanPlayer(int uuid){
		if(IsHost() && uuid != 1){
			BannedIps.Add(GetIp(uuid));
			(Game.GameNode.Multiplayer as SceneMultiplayer).DisconnectPeer(uuid);
			PlayerDisconnected(uuid);
		}
	}

	public static string GetIp(int uuid){
		return (Game.GameNode.Multiplayer.MultiplayerPeer as ENetMultiplayerPeer).GetPeer(uuid).GetRemoteAddress();
	}

	public static bool IsOnlinePeer(){
		return Game.GameNode.Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer;
	}


	public void ReturnToLobby(){
		Game.GameNode.GetTree().Paused = false;
		Online.RemoveDisconnectedPlayerInfos();
		if(Online.IsOnline) Game.TotalPlayers = Game.PlayerDatas.Count;
		MenuScene.MenuToLoad = "Online/OnlineLobby";
		SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
	}

	public static void FailedToStart(MultiplayerPeer peer, Error error){
		peer.Close();
		Game.PlayerDatas = new List<PlayerData>();
		Node pingGetter = Game.GameNode.GetNodeOrNull("PingGetter");
		if(pingGetter != null) pingGetter.QueueFree();
		MenuScene.MenuToLoad = "Online/OnlineMenu";
		if(Game.GameNode.Multiplayer.MultiplayerPeer != null){
			Game.GameNode.Multiplayer.MultiplayerPeer.Dispose();
			Game.GameNode.Multiplayer.MultiplayerPeer = null;
			Game.GameNode.Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
		}
		GD.Print(error.ToString());
		SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
	}

	public enum TransferChannelEnum : int{
		Default = 0,
		SendLaunch, SendSlam, SuccessfulStomp, Item, PlayerText,
		PlayerFlip,
		SportBall,
		LaunchParticle, SlamParticle, PopParticle, DeathParticle, BounceParticle, Trail,
		PingGetter,SendHostPing,
	}

	public enum NetworkType{
		Offline,Direct,Steam
	}
}