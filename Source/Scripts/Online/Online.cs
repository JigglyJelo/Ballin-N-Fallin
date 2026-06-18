using Godot;
using System.Collections.Generic;

public partial class Online{
    public static bool IsOnline = false;
    public static NetworkType Network = NetworkType.Direct;
    public static string username = "Player";
    public static string HostUsername;
    public const int USERNAME_LENGTH = 15;
    public const ushort DEFAULT_PORT = 8411;
    public static string NorayHostOid = ""; 

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
    public static List<string> BannedIps = new List<string>();
    
    private static float buffer = 1;
    public static float Buffer{
        get{return buffer;}
        set{buffer = Mathf.Clamp(value, 0f, 1f);} 
    }

    public static PlayerData GetClientsPlayerData(){
        foreach(PlayerData playerData in Game.PlayerDatas){
            if(playerData.UUID == GetUUID()) return playerData;
        }
        return null;
    }

    public static int GetUUID(){
        return Game.GameNode.Multiplayer.GetUniqueId();
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
        return Game.GameNode.Multiplayer.MultiplayerPeer?.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
    }
    
    public static bool HasDisconnected(){
        return Game.GameNode.Multiplayer.MultiplayerPeer == null || 
               Game.GameNode.Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected;
    }

    public static void Disconnect(string reason) {
        Game.GameNode.GetNodeOrNull("NohubHostManager")?.QueueFree();
        Game.GameNode.GetNodeOrNull("PingGetter")?.QueueFree();
        MenuScene.MenuToLoad = "Online/OnlineMenu";
        IsOnline = false;

        MultiplayerPeer peer = Game.GameNode.Multiplayer.MultiplayerPeer;
        if (peer != null && peer is not OfflineMultiplayerPeer) {
            peer.Close();
            peer.Dispose();
        }
        
        Game.GameNode.Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
        Game.PlayerDatas = new List<PlayerData>();
        
        GD.Print(reason);
        Game.GameNode.GetTree().Paused = false;
        Engine.TimeScale = 1;
        Game.Paused = false;
        PingGetter.LastPing = 0;
        SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
    }

    public static void Disconnect() => Disconnect("Disconnected");

    public static void PlayerDisconnected(long id){
        if(IsHost()){
            bool removePlayer = Game.Players == null || Game.Players.Length == 0;
            Game.TellClientsWhatToDoAboutDisconnectedPlayer(removePlayer, (int)id);
            NohubHostManager.UpdateLobbyData();
        }
    }


    public static void RemoveDisconnectedPlayerInfos(){
        if(IsOnline && Game.DisconnectedDatas.Count > 0){
            foreach(PlayerData playerInfo in Game.DisconnectedDatas){
                int index = Game.PlayerDatas.IndexOf(playerInfo);
                Game.PlayerDatas.Remove(playerInfo);
                
                List<int> scores = new List<int>(Tour.PlayerScores);
                if(index != -1) scores.RemoveAt(index);
                for(int i = 0; i < scores.Count; i++){
                    Tour.PlayerScores[i] = scores[i];
                }
            }
            Game.DisconnectedDatas = new List<PlayerData>();
            Game.TotalPlayers = (byte)Game.PlayerDatas.Count;
            if(Game.CurrentScene == Game.SceneType.Game && IsHost()){
                Game.GameNode.GetNode<PlayerSync>("Scene/PlayerSynchronizer").ResetSyncArrayLengths();
            }
        }
    }

    public static bool PeerIsActive(){
        return Game.GameNode.Multiplayer.MultiplayerPeer != null && 
               Game.GameNode.Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected && 
               IsOnlinePeer();
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
        // PREVENTS BANNING NORAY RELAY SERVERS
        if (Network == NetworkType.Noray) return "Noray-Peer-" + uuid.ToString();

        if (Game.GameNode.Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enetPeer) {
            return enetPeer.GetPeer(uuid).GetRemoteAddress();
        }
        return "Unknown-IP-" + uuid.ToString(); 
    }

    public static bool IsOnlinePeer() => Game.GameNode.Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer;

    public void ReturnToLobby(){
        Game.GameNode.GetTree().Paused = false;
        RemoveDisconnectedPlayerInfos();
        if(IsOnline) Game.TotalPlayers = Game.PlayerDatas.Count;
        MenuScene.MenuToLoad = "Online/OnlineLobby";
        SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
    }

    public static void FailedToStart(MultiplayerPeer peer, Error error){
        Disconnect("Failed to start: " + error);
    }

    public enum TransferChannelEnum : int{
        Default = 0, SendLaunch, SendSlam, SuccessfulStomp, Item, PlayerText,
        PlayerFlip, SportBall, LaunchParticle, SlamParticle, PopParticle, DeathParticle, BounceParticle, Trail,
        PingGetter,SendHostPing, Chat
    }

    public enum NetworkType{
        Offline, Direct, Steam, Noray
    }

    //Probably going to remove these at some point and maybe try limited Netfox rollback instead
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
                    //Create bounce particle effects at simulated position
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
                        if(lastCollision != null){
                            if(lastCollision.HasSignal("body_exited")) lastCollision.EmitSignal("body_exited",rb);
                            rb.EmitSignal("body_exited",lastCollision);
                        }
                        if(collidedPhysicsBody.HasSignal("body_entered")) collidedPhysicsBody.EmitSignal("body_entered",rb);
                        rb.EmitSignal("body_entered",collidedPhysicsBody);
                        lastCollision = collidedPhysicsBody;
                    }
                }
            }else if(lastCollision != null){
                if(lastCollision.HasSignal("body_exited")) lastCollision.EmitSignal("body_exited",rb);
                rb.EmitSignal("body_exited",lastCollision);
                lastCollision = null;
            }
            
            if(playerPrediction){
                Player player = rb.GetParent() as Player;
                getPlayerAreaOverlaps(false);
                if(i != physicsTicks-1){ 
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
                if(uuid != predictedPlayer.OwnerId && player.OwnerId != 1 && !Game.DisconnectedDatas.Contains(Game.PlayerDatas[i])){
                    predictedPlayer.Trail.RpcId(uuid,nameof(predictedPlayer.Trail.SyncTrail),trailPointsToSend);
                }
            }
        }
        
        //Local functions
        void getPlayerAreaOverlaps(bool firstIteration){
            Player player = rb.GetParent() as Player;
            foreach(Area2D area in areasInScene){
                foreach(Node node in area.GetChildren()){
                    if(node is CollisionShape2D collisionShape){
                        if(!areasEntered.Contains(area) && playerInsideArea(player,collisionShape)){
                            if(!firstIteration){ 
                                if(area.IsInGroup("Stop Prediction")){
                                    stopPrediction = true;
                                }
                                area.EmitSignal("body_entered",rb);
                            }
                            areasEntered.Add(area);
                            GD.Print("Entered " + area.Name);
                        }else if(areasEntered.Contains(area) && !playerInsideArea(player,collisionShape)){
                            area.EmitSignal("body_exited",rb);
                            areasEntered.Remove(area);
                            GD.Print("Exited " + area.Name);
                        }
                        break; 
                    }
                }
            }
        }
        
        bool playerInsideArea(Player player,CollisionShape2D collisionShape){
            float playerRadius = PlayerPhysics.RADIUS * player.PlayerScale;
            switch(collisionShape.Shape){
                case RectangleShape2D rectangleShape:{
                    Transform2D rectTransform = collisionShape.GlobalTransform;
                    Vector2 playerPosition;
                    Vector2 rectSize = rectangleShape.Size;
                    Vector2 rectTopLeft;
                    if(Mathf.IsZeroApprox(rectTransform.Rotation)){ 
                        Vector2 rectCenter = collisionShape.GlobalPosition;
                        rectTopLeft = rectCenter - (rectSize / 2);
                        playerPosition = player.Rb.GlobalPosition;
                    }else{ 
                        Transform2D rectInverseTransform = rectTransform.AffineInverse();
                        playerPosition = rectInverseTransform.BasisXform(player.Rb.GlobalPosition);
                        rectTopLeft = -rectSize / 2;
                    }
                    float closestX = Mathf.Clamp(playerPosition.X, rectTopLeft.X, rectTopLeft.X + rectSize.X);
                    float closestY = Mathf.Clamp(playerPosition.Y, rectTopLeft.Y, rectTopLeft.Y + rectSize.Y);
                    float distanceX = playerPosition.X - closestX;
                    float distanceY = playerPosition.Y - closestY;
                    float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
                    return distanceSquared <= (playerRadius * playerRadius);
                }
                case CircleShape2D circleShape:{
                    Vector2 circleCenter = collisionShape.GlobalPosition;
                    float circleRadius = circleShape.Radius;
                    float distanceX = player.Rb.GlobalPosition.X - circleCenter.X;
                    float distanceY = player.Rb.GlobalPosition.Y - circleCenter.Y;
                    float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
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
}