using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TrashCompactor : Node2D{
    [Export]
    public float TrashLaunchAngle = MathF.PI/2;
    private PackedScene[] Trash;
    private float spawnInterval = 2f;
    public Random BallColorRandom = new Random(Game.TotalPlayers * Tour.CurrentTour.PointsToWin);
    private readonly Tuple<float, float>[] SPAWN_TIMES = new Tuple<float, float>[]{
        new Tuple<float, float>(0.2f, 150),
        new Tuple<float, float>(0.25f, 120),
        new Tuple<float, float>(0.33f, 105),
        new Tuple<float, float>(0.4f, 90),
        new Tuple<float, float>(0.5f, 75),
        new Tuple<float, float>(0.66f, 60),
        new Tuple<float, float>(0.75f, 45),
        new Tuple<float, float>(1, 30),
        new Tuple<float, float>(1.2f, 22),
        new Tuple<float, float>(1.5f, 15),
        new Tuple<float, float>(1.75f, 10),
        new Tuple<float, float>(1.8f, 5),
    };   
        
    //Online variables
    private int syncTimer = 0;
	private const int SYNC_INTERVAL = 4;
	private const int ROTATION_SYNC_INTERVAL = SYNC_INTERVAL*2;
    private byte[] positionArray, rotationArray;
    //Only 256 trash can be spawned at once since each needs a unique byte Id
    public Dictionary<byte,Trash> SpawnedTrash = new Dictionary<byte, Trash>();
    public override void _Ready(){
        Trash = new PackedScene[]{
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Bottle.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Cone.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Box.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Crate.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Barrel.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Girder.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Tire.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/Safe.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/TrashSportBall.tscn"),
            GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/Trash/TrashPlayerBall.tscn"),
        };
    }

    public override void _PhysicsProcess(double delta){
        if(Online.IsHost()){
            if(Survival.Timer >= spawnInterval){
                SpawnTrash();
                Survival.Timer = 0;
                foreach(Tuple<float,float> spawnTime in SPAWN_TIMES){
                    if(spawnInterval > spawnTime.Item1 && Survival.TotalTime >= spawnTime.Item2){
                        spawnInterval = spawnTime.Item1;
                        GD.Print("Time: " + spawnTime.Item2 + " Secs per Trash: " + spawnInterval);
                        break;
                    } 
                }  
            }
            if(Online.IsOnline){
                syncTimer++;
                if(syncTimer == SYNC_INTERVAL || syncTimer == ROTATION_SYNC_INTERVAL){
                    SendTrashInfo();
                }
            }
        }
    }

    public void SpawnTrash(){ 
        int trashIndex = Game.Random.Next(0,Trash.Length);
        for(int i = (int)(Survival.TotalTime / 30); i > 1; i--){
            if(trashIndex == Trash.Length-1) break;
            else if(Game.Random.Next(0,5) == 1) trashIndex++;
        }
        
        float radAngle = (TrashLaunchAngle)+(Game.Random.NextSingle()*MathF.PI); //Random angle in radians [π/2 , 3π/2)
        float rotation = Game.Random.NextSingle() * 2 * MathF.PI; //Random angle in radians [0 , 2π)
        float force;
        force = Game.Random.Next(100,3000);
        const float SPEED_UP_TIME = 30;
        force += Survival.TotalTime < SPEED_UP_TIME ? Survival.TotalTime * 2 : MathF.Pow(Survival.TotalTime,2)/(SPEED_UP_TIME*2);
        HashSet<byte> keys = new HashSet<byte>(SpawnedTrash.Keys);
        byte? newItemId = ItemSynchronizer.GetUnusedItemId(keys);
        if(newItemId != null){
            Rpc(nameof(SpawnTrash),trashIndex,radAngle,rotation,force,(byte)newItemId);
        }else{ //Too much trash is spawned delete oldest to make room
            byte firstTrashId = SpawnedTrash.ElementAt(0).Key;
            Rpc(nameof(RemoveTrash),firstTrashId);
            Rpc(nameof(SpawnTrash),trashIndex,radAngle,rotation,force,firstTrashId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnTrash(byte trashType,float angle,float rotation,float force,byte id){
        Trash trash = Trash[trashType].Instantiate<Trash>();
        Vector2 vectorAngle = Vector2.FromAngle(angle);
        trash.Name = "Trash" + id;
        SpawnedTrash.Add(id,trash);
        trash.Rb = trash.GetNode<InterpolatedBody>("RigidBody2D");
        trash.Rb.SetDeferred("rotation",rotation);
        trash.Rb.CallDeferred("apply_impulse",vectorAngle * force);
        trash.Rb.GlobalPosition = GlobalPosition;
        trash.Rb.SetDeferred("global_position",GlobalPosition);
        trash.Rb.VisualsNode.GlobalPosition = new Vector2(694207,-694207);
        trash.Rb.SkipInterpolation();
        AddChild(trash);
    }

    private void SendTrashInfo(){
        int trashCount = SpawnedTrash.Count;
        if(trashCount == 0){
            if(syncTimer == ROTATION_SYNC_INTERVAL) syncTimer = 0;
            return;
        } 
        //Prepare arrays for rpc
        byte[] trashIds = new byte[trashCount];
        float[] xPosArray = new float[trashCount];
        float[] yPosArray = new float[trashCount];
        float[] xVelArray = new float[trashCount];
        float[] yVelArray = new float[trashCount];
        for(int i = 0; i < trashCount; i++){
            KeyValuePair<byte,Trash> trashPair = SpawnedTrash.ElementAt(i);
            try{
                trashIds[i] = trashPair.Key;
                RigidBody2D trash = trashPair.Value.Rb;
                xPosArray[i] = trash.GlobalPosition.X;
                yPosArray[i] = trash.GlobalPosition.Y;
                xVelArray[i] = trash.LinearVelocity.X;
                yVelArray[i] = trash.LinearVelocity.Y;
            }catch(Exception ex){
                GD.Print(trashPair.Value.Name.ToString() + " " + ex.ToString());
            }
        }
        //Rpcs
        byte[] positionData = new byte[(trashCount*17)+2];
        Buffer.BlockCopy(trashIds,0,positionData,0,trashIds.Length);
        Buffer.BlockCopy(xPosArray,0,positionData,trashCount,xPosArray.Length*4);
        Buffer.BlockCopy(yPosArray,0,positionData,trashCount*5,yPosArray.Length*4);
        Buffer.BlockCopy(xVelArray,0,positionData,trashCount*9,xVelArray.Length*4);
        Buffer.BlockCopy(yVelArray,0,positionData,trashCount*13,yVelArray.Length*4);
        ushort positionUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.TrashPosition);
        BitConverter.GetBytes(positionUpdate).CopyTo(positionData, positionData.Length-2);
        if(syncTimer == SYNC_INTERVAL){
            Rpc(nameof(SyncPosition),positionData);
            UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.TrashPosition);
		}else if(syncTimer == ROTATION_SYNC_INTERVAL){
            Rpc(nameof(SyncPosition),positionData);
            UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.TrashPosition);
            float[] rotations = new float[trashCount];
            float[] angVel = new float[trashCount];
            for(int i = 0; i < trashCount; i++){
                KeyValuePair<byte,Trash> trashPair = SpawnedTrash.ElementAt(i);
                RigidBody2D trash = trashPair.Value.Rb;
                rotations[i] = trash.Rotation;
                angVel[i] = trash.AngularVelocity;
            }
            byte[] rotationData = new byte[(trashCount*9)+2];
            Buffer.BlockCopy(trashIds,0,rotationData,0,trashIds.Length);
            Buffer.BlockCopy(rotations,0,rotationData,trashCount,rotations.Length*4);
            Buffer.BlockCopy(angVel,0,rotationData,trashCount*5,angVel.Length*4);
            ushort rotationUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.TrashRotation);
            BitConverter.GetBytes(rotationUpdate).CopyTo(rotationData, rotationData.Length-2);
            Rpc(nameof(SyncRotation),rotationData);
            UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.TrashRotation);
			syncTimer = 0;
		}
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]//Ordered, TransferChannel = (int)Online.TransferChannelEnum.TrashPosition
	private void SyncPosition(byte[] trashData){ //Convert to one byte arraybyte[] trashIds,Vector2[] positions,Vector2[] velocities
        ushort update = BitConverter.ToUInt16(trashData, trashData.Length-2);
        if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.TrashPosition,update)){
            int trueLength = (trashData.Length-2) / 17;
            int xPosOffset = trueLength; // X positions follow IDs.
            int yPosOffset = xPosOffset + (trueLength * 4); // Y positions follow X positions.
            int xVelOffset = yPosOffset + (trueLength * 4); // X velocities follow Y positions.
            int yVelOffset = xVelOffset + (trueLength * 4);

            for(int i = 0; i < trueLength; i++){
                byte id = trashData[i];
                float posX = BitConverter.ToSingle(trashData, xPosOffset + (i * 4));
                float posY = BitConverter.ToSingle(trashData, yPosOffset + (i * 4));
                float velX = BitConverter.ToSingle(trashData, xVelOffset + (i * 4));
                float velY = BitConverter.ToSingle(trashData, yVelOffset + (i * 4));
                try{
                    InterpolatedBody trash = SpawnedTrash[id].Rb;
                    Vector2 newPosition = new Vector2(posX,posY);
                    if(trash.GlobalPosition.IsZeroApprox()){
                        trash.GlobalPosition = new Vector2(694207,-694207); //Prevent trash from appearing at 0,0 if the data was sent right as the trash spawned
                        trash.NetworkPosition = new Vector2(694207,-694207);
                    }else{
                        //trash.GlobalPosition = newPosition;
                        //trash.GlobalPosition = trash.GlobalPosition;
                        trash.NetworkPosition = newPosition;
                    }
                    //trash.LinearVelocity = new Vector2(velX,velY);
                    trash.NetworkVelocity = new Vector2(velX,velY);
                }catch{}
            }
        }
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]//Ordered, TransferChannel = (int)Online.TransferChannelEnum.TrashRotation
	private void SyncRotation(byte[] trashData){
        ushort update = BitConverter.ToUInt16(trashData, trashData.Length-2);
        if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.TrashRotation,update)){
            int trueLength = (trashData.Length-2) / 9;
            int rotationOffset = trueLength; // Rotations follow IDs.
            int angularVelOffset = rotationOffset + (trueLength * 4);
            for(int i = 0; i < trueLength; i++){
                byte id = trashData[i];
                float rotation = BitConverter.ToSingle(trashData, rotationOffset + (i * 4));
                float angularVelocity = BitConverter.ToSingle(trashData, angularVelOffset + (i * 4));
                try{
                    RigidBody2D trash = SpawnedTrash[id].Rb;
                    trash.Rotation = rotation;
                    trash.AngularVelocity = angularVelocity;
                }catch{}
            }
        }
	}

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RemoveTrash(byte trashId){
        Trash trash = SpawnedTrash[trashId];
        SpawnedTrash.Remove(trashId);
        trash.QueueFree();
    }
}