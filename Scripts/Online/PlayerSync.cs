using System;
using Godot;

public partial class PlayerSync : Node{
    private int positionSyncTimer = 0;
	private readonly int POSITION_SYNC_INTERVAL = Engine.PhysicsTicksPerSecond/30; //30 Times per second
	private int visualSyncTimer = 0;
	public static readonly int VISUAL_SYNC_INTERVAL = Engine.PhysicsTicksPerSecond/15; //15 Times per second
	private float[] xVelocities;
	private float[] yVelocities;
	private float[] xPositions;
	private float[] yPositions;
	private float[] angularVelocities;
	private byte[] velocities;
	private byte[] positions_angularVelocities; //X & Y of Vec3 is Position Z is Angular Velocity
	private byte[] chargeScales;
	private bool[] canLaunches;

    public override void _Ready(){
		if(!Online.IsOnline){
			SetPhysicsProcess(false);
			Free();
		}else{
			if(Online.IsHost()){
				ResetSyncArrayLengths();
			}
		}
    }

    public override void _PhysicsProcess(double delta){
		if(!Mode.Finished && !GetTree().Paused && Online.IsOnlinePeer() && Game.Players != null && Game.Players.Length != 0){
			if(Online.IsHost()){
				ServerSyncUpdate();
			}else{
				ClientSyncUpdate();
			}
		}
    }

	private void ClientSyncUpdate(){
		if(Game.Players != null && Game.Players.Length > 0){
			bool visualSyncTime = ++visualSyncTimer == VISUAL_SYNC_INTERVAL;
			if(visualSyncTime){
				for(int i = 0; i < Game.TotalPlayers; i++){
					Player player = Game.Players[i];
					if(player.OwnsPlayer() && visualSyncTime && !GetTree().Paused){
						if(player.RawInputVector.X == 0f && player.RawInputVector.Y == 0f){
							sendArrowData(3);
						}else if(player.RawInputVector.LengthSquared() >= 0.9f*0.9f){
							sendArrowData(4);
						}else{
							sendArrowData(5);
						}
						break;
					}

					void sendArrowData(int size){
						byte launchPower = (byte)(player.LaunchPower / Player.MAX_LAUNCH_POWER * byte.MaxValue);
						ushort chargeUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.ClientUnreliableChannel.PlayerArrow);
						byte[] arrowData = new byte[size];
						arrowData[0] = launchPower;
						switch(size){
							case 4:
								arrowData[1] = (byte)(player.InputVector.Angle() / (2*Math.PI) * 255);
								break;
							case 5:
								arrowData[1] = (byte)((sbyte)(player.RawInputVector.X * 127));
								arrowData[2] = (byte)((sbyte)(player.RawInputVector.Y * 127));
								break;
						}
						BitConverter.GetBytes(chargeUpdate).CopyTo(arrowData, arrowData.Length-2);
						RpcId(1,nameof(SyncChargeScale),arrowData);
						UnreliableManager.ClientIncrementUpdate(UnreliableManager.ClientUnreliableChannel.PlayerArrow);
					}
				}
				visualSyncTimer = 0;
			}
		}
	}

    private void ServerSyncUpdate(){
		//Sync Control Variables
		bool positionSyncTime = ++positionSyncTimer == POSITION_SYNC_INTERVAL;
		bool visualSyncTime = ++visualSyncTimer == VISUAL_SYNC_INTERVAL;
		if(xVelocities.Length != Game.TotalPlayers){
			ResetSyncArrayLengths();
		}

		if(positionSyncTime){
			for(int i = 0; i < Game.TotalPlayers; i++){
				Player player = Game.Players[i];
				xPositions[i] = player.Rb.GlobalPosition.X;
				yPositions[i] = player.Rb.GlobalPosition.Y;
				angularVelocities[i] = player.Rb.AngularVelocity;
			}
		}

		for(int i = 0; i < Game.TotalPlayers; i++){
            Player player = Game.Players[i];
			xVelocities[i] = player.Rb.LinearVelocity.X;
			yVelocities[i] = player.Rb.LinearVelocity.Y;
			if(visualSyncTime){
				chargeScales[i] = (byte)(player.LaunchPower / (Player.MAX_LAUNCH_POWER/255f));
				canLaunches[i] = player.CanLaunch;
			}
		}

		Buffer.BlockCopy(xVelocities,0,velocities,0,xVelocities.Length*4);
		Buffer.BlockCopy(yVelocities,0,velocities,xVelocities.Length*4,yVelocities.Length*4);
		ushort velUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.PlayerVelocity);
		BitConverter.GetBytes(velUpdate).CopyTo(velocities, (xVelocities.Length + yVelocities.Length) * 4);

		if(positionSyncTime){ //[xxxx*P yyyy*P aaaa*P uu] x=X Position y=Y Position a=Angular Velocity P=Players u=update
			Buffer.BlockCopy(xPositions,0,positions_angularVelocities,0,xPositions.Length*4);
			Buffer.BlockCopy(yPositions,0,positions_angularVelocities,xPositions.Length*4,yPositions.Length*4);
			Buffer.BlockCopy(angularVelocities,0,positions_angularVelocities,xPositions.Length * 4 + yPositions.Length * 4,angularVelocities.Length*4);
			ushort posUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.PlayerPosition);
			BitConverter.GetBytes(posUpdate).CopyTo(positions_angularVelocities, (xPositions.Length + yPositions.Length + angularVelocities.Length) * 4);
		}

		//|8 Bytes per Player|60 TPS|480 Bytes per Player per Second|
		Rpc(nameof(SyncLinearVelocities),velocities);
		UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.PlayerVelocity);
		if(positionSyncTime){
			//|12 Bytes per Player|30 TPS|360 Bytes per Player per Second|
			Rpc(nameof(SyncPlayers),positions_angularVelocities);
			UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.PlayerPosition);
			positionSyncTimer = 0;
		}
		
		if(visualSyncTime){
			byte[] inputVectors = new byte[Game.TotalPlayers+2]; //[i*P uu] i=inputVector P=Players u=update
			for(int i = 0; i < inputVectors.Length-2; i++){
				Player player = Game.Players[i];
				if(player.InputVector.Equals(Vector2.Zero)){
					inputVectors[i] = 255;
				}else{
					inputVectors[i] = (byte)( player.InputVector.Angle() / (2*MathF.PI) * 254);
					if(inputVectors[i] == 255) inputVectors[i] = 254;
				}
			}
			ushort angleUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.PlayerDirection);
			BitConverter.GetBytes(angleUpdate).CopyTo(inputVectors, inputVectors.Length-2);
			byte[] chargeScales_canLaunches = new byte[chargeScales.Length+3]; //[s*P c uu] s=ChargeScales P=Players c=Canlaunches u=Update
			for(int i = 0; i < chargeScales.Length; i++){
				chargeScales_canLaunches[i] = chargeScales[i];
    			if(canLaunches[i]){
       				// Set the bit at position i
        			chargeScales_canLaunches[chargeScales_canLaunches.Length-3] |= (byte)(1 << i);
				}
    		}
			ushort arrowUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.PlayerArrow);
			BitConverter.GetBytes(arrowUpdate).CopyTo(chargeScales_canLaunches, chargeScales_canLaunches.Length-2);
			//|1 Byte per Player + 1 Constant Byte|15 TPS|15 Bytes per Player per Second + 15 Constant bytes per Second|
			Rpc(nameof(SyncArrows),chargeScales_canLaunches);
			UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.PlayerArrow);
			Rpc(nameof(SyncInputVectors),inputVectors); //1 Byte per player
			UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.PlayerDirection);
			visualSyncTimer = 0;
		}
	}

    //Sync Variable Functions
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncLinearVelocities(byte[] velocities){ //Vector2[] velocities
		ushort update = BitConverter.ToUInt16(velocities, velocities.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.PlayerVelocity,update)){
			if(Game.Players.Length > 0){
				int trueLength = (velocities.Length-2)/8;
				int yVelOffset = trueLength * 4;
				for(int i = 0; i < trueLength; i++){
					Player player = Game.Players[i];
					if(!Online.IsHost() && Online.IsRpcFromHost() && player.TicksToIgnore == 0){
						float velX = BitConverter.ToSingle(velocities, i * 4);
            			float velY = BitConverter.ToSingle(velocities, yVelOffset + (i * 4));
						//player.Rb.LinearVelocity = new Vector2(velX,velY);
						player.Rb.NetworkVelocity = new Vector2(velX,velY);
					}
				}
			}
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncPlayers(byte[] positions_angularVelocities){ //byte[] positions_anglularVelocites
		//First (trueLength*4) bytes are x positions, then y positions, and then angular velocites Last two bytes are update
		ushort update = BitConverter.ToUInt16(positions_angularVelocities, positions_angularVelocities.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.PlayerPosition,update)){
			if(Game.Players.Length > 0){
				int trueLength = (positions_angularVelocities.Length-2)/12;
        		int yPosOffset = trueLength * 4; // Y positions follow X positions.
        		int angVelOffset = yPosOffset + (trueLength * 4); // Angular Velocities follow Y positions.
				for(int i = 0; i < trueLength; i++){
					Player player = Game.Players[i];
					if(!Online.IsHost() && Online.IsRpcFromHost() && player.TicksToIgnore == 0){
						bool teleport = Level.IsPositionOffscreenOrDead(player.Rb.GlobalPosition);
						float posX = BitConverter.ToSingle(positions_angularVelocities, i * 4);
            			float posY = BitConverter.ToSingle(positions_angularVelocities, yPosOffset + (i * 4));
						//player.Rb.AngularVelocity = BitConverter.ToSingle(positions_angularVelocities, angVelOffset + (i * 4));
						player.Rb.NetworkAngularVelocity = BitConverter.ToSingle(positions_angularVelocities, angVelOffset + (i * 4));
						Vector2 newPosition = new Vector2(posX,posY);
						if(Level.IsPositionOffscreenOrDead(newPosition)) teleport = true;
						//player.Rb.GlobalPosition = newPosition;
						player.Rb.NetworkPosition = newPosition;
						if(teleport) player.Rb.SkipInterpolation();
					}
				}
			}
		}
	}
	//Syncs the Player's Launch Power (Used for arrow scale n Flame) using 1 byte per Player
	//chargeScale is divided by 255 to get a decimal that will be multiplied by MAX_LAUNCH_POWER to get the power
	//CanLaunch is last byte in array each bit of said byte is the bool value starting from most sig to least sig bit
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)] //Ordered, TransferChannel = (int)Online.TransferChannelEnum.PlayerArrow)]
	private void SyncArrows(byte[] chargeScales_canLaunches){
		ushort update = BitConverter.ToUInt16(chargeScales_canLaunches, chargeScales_canLaunches.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.PlayerArrow,update)){
			if(Game.Players.Length > 0){
				//Convert final byte in array to bool array of canLaunches
				bool[] canLaunches = new bool[8];
				byte canLaunchesByte = chargeScales_canLaunches[chargeScales_canLaunches.Length-3];
        		for(int i = 0; i < 8; i++){
            		// Check each bit and convert to bool
            		canLaunches[i] = (canLaunchesByte & (1 << i)) != 0;
        		}
				for(int i = 0; i < chargeScales_canLaunches.Length-3; i++){
					Player player = Game.Players[i];
					if(!player.OwnsPlayer()){
						player.LaunchPower = chargeScales_canLaunches[i]/255f * Player.MAX_LAUNCH_POWER;
						player.CanLaunch = canLaunches[i];
					}
				}
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncInputVectors(byte[] angles){
		ushort update = BitConverter.ToUInt16(angles, angles.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.PlayerDirection,update)){
			if(!Online.IsHost() && Game.Players.Length > 0){
				for(int i = 0; i < angles.Length-2; i++){
					Player player = Game.Players[i];
					if(!player.OwnsPlayer()){
						if(angles[i] != 255){
							float fAngle = angles[i]/254f * (2*MathF.PI);
							player.InputVector = Vector2.FromAngle(fAngle);
							player.RawInputVector = player.InputVector;
						}else{
							player.InputVector = Vector2.Zero;
							player.RawInputVector = player.InputVector;
						}
					}
				}
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void SyncChargeScale(byte[] arrowData){ // 3-5 Byte Array: First byte is charge scalenext (0, 1 or 2) bytes are angle and last two are update
		int id = Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId();
		ushort update = BitConverter.ToUInt16(arrowData, arrowData.Length-2);
		if(Online.IsHost()){
			if(UnreliableManager.IsNewerRpc(UnreliableManager.ClientUnreliableChannel.PlayerArrow,id,update)){
				for(int i = 0; i < Game.PlayerDatas.Count; i++){
					if(Game.PlayerDatas[i].UUID == id){
						Player player = Game.Players[i];
						player.LaunchPower = arrowData[0] / 255f * Player.MAX_LAUNCH_POWER; // Scale back to original range
						switch(arrowData.Length){
							case 3:
								player.InputVector = Vector2.Zero;
								player.RawInputVector = player.InputVector;
								break;
							case 4:
								float fAngle = arrowData[1]/255f * (2*MathF.PI);
								player.InputVector = Vector2.FromAngle(fAngle);
								player.RawInputVector = player.InputVector;
								break;
							case 5:
								sbyte x = (sbyte)arrowData[1];
								sbyte y = (sbyte)arrowData[2];
								player.RawInputVector = new Vector2(x/127f,y/127f);
								player.InputVector = player.RawInputVector.Normalized();
								break;
							default:
								GD.PrintErr(Game.PlayerDatas[i].Username + "Sent invalid arrow packet. Size: " + arrowData.Length);
								break;
						}
						break;
					}
				}
			}
		}else{
			GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
		}
	}
	//Call after a player DCs before round start to ensure accurate array sizes
	public void ResetSyncArrayLengths(){
		xPositions = new float[Game.TotalPlayers];
		yPositions = new float[Game.TotalPlayers];
		xVelocities = new float[Game.TotalPlayers];
		yVelocities = new float[Game.TotalPlayers];
		angularVelocities = new float[Game.TotalPlayers];
		positions_angularVelocities = new byte[(Game.TotalPlayers*12)+2];
		velocities = new byte[(Game.TotalPlayers*8)+2];
		chargeScales = new byte[Game.TotalPlayers];
		canLaunches = new bool[Game.TotalPlayers];
	}
}