using System;
using Godot;

public partial class SportBall : Node{
	private int syncTimer = 0;
	private const int SYNC_INTERVAL = 2;
	public InterpolatedBody Rb;
	public Node2D Smoother;
	private Sprite2D shadingSprite;

    public override void _Ready(){
        Rb = GetNode<InterpolatedBody>("Ball");
		Smoother = GetNode<Node2D>("Smoothing2D");
		Smoother.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Mode Stuff/" + Mode.EnumToString(Game.CurrentMode) + "/" + Mode.EnumToString(Game.CurrentMode) + "Sprites.tscn").Instantiate());
		shadingSprite = GetNode<Sprite2D>("Smoothing2D/Sprites/Shading");
		Mode.AddCameraTarget(Smoother);
    }

    public override void _Process(double delta){
		shadingSprite.GlobalRotation = 0;
    }

    public override void _PhysicsProcess(double delta){
		float magnitudeSquared = Rb.LinearVelocity.LengthSquared();
		if(magnitudeSquared > 5000*5000){
			float angle = Rb.LinearVelocity.Angle();
			Rb.LinearVelocity = Vector2.FromAngle(angle) * 5000;
		}
		if(Online.IsOnline && Online.IsHost() && !GetTree().Paused){
			syncTimer++;
			byte[] ballData;
			ushort update = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.SportBall);
        	if(syncTimer == SYNC_INTERVAL){
				ballData = new byte[18];
				BitConverter.GetBytes(Rb.GlobalPosition.X).CopyTo(ballData, 0);
				BitConverter.GetBytes(Rb.GlobalPosition.Y).CopyTo(ballData, 4);
				BitConverter.GetBytes(Rb.LinearVelocity.X).CopyTo(ballData, 8);
				BitConverter.GetBytes(Rb.LinearVelocity.Y).CopyTo(ballData, 12);
				BitConverter.GetBytes(update).CopyTo(ballData, 16);
				Rpc(nameof(SyncBall),ballData);
				syncTimer = 0;
			}else{
				ballData = new byte[10];
				BitConverter.GetBytes(Rb.LinearVelocity.X).CopyTo(ballData, 0);
				BitConverter.GetBytes(Rb.LinearVelocity.Y).CopyTo(ballData, 4);
				BitConverter.GetBytes(update).CopyTo(ballData, 8);
				Rpc(nameof(SyncVelocity),ballData);
			}
			UnreliableManager.HostIncrementLastUpdate(UnreliableManager.UnreliableChannel.SportBall);
		}
    }

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.SportBall)]
	public void StartSpawnTween(){
		Node2D sportBallSprite = Smoother.GetNode<Node2D>("Sprites");
        sportBallSprite.Scale = Vector2.Zero;
		Tween scaleTween = CreateTween();
		scaleTween.TweenProperty(sportBallSprite,"scale",Vector2.One,0.125);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]//Ordered, TransferChannel = (int)Online.TransferChannelEnum.SportBall
	private void SyncBall(byte[] ballData){
		ushort update = BitConverter.ToUInt16(ballData, ballData.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.SportBall,update)){
			float xPos = BitConverter.ToSingle(ballData, 0);
			float yPos = BitConverter.ToSingle(ballData, 4);
			Vector2 position = new Vector2(xPos,yPos);
			float xVel = BitConverter.ToSingle(ballData, 8);
			float yVel = BitConverter.ToSingle(ballData, 12);
			Vector2 velocity = new Vector2(xVel,yVel);
			Rb.NetworkPosition = position;
			Rb.NetworkVelocity = velocity;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]//Ordered, TransferChannel = (int)Online.TransferChannelEnum.SportBall
	private void SyncVelocity(byte[] velocityData){
		ushort update = BitConverter.ToUInt16(velocityData, velocityData.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.SportBall,update)){
			float xVel = BitConverter.ToSingle(velocityData, 0);
			float yVel = BitConverter.ToSingle(velocityData, 4);
			Vector2 velocity = new Vector2(xVel,yVel);
			Rb.NetworkVelocity = velocity;
		}
	}

	private void PredictPosition(){
		GD.Print("New prediction");
		int ticks = (int)((PingGetter.LastPing / (1000.0/Engine.PhysicsTicksPerSecond)));// /2
		double delta = GetPhysicsProcessDeltaTime();
		PhysicsMaterial physicsMaterial = Rb.PhysicsMaterialOverride;
		Vector2 currentVelocity = Rb.LinearVelocity;
		for(int i = 0; i < ticks; i++){
			currentVelocity += new Vector2(0,(float)((float)ProjectSettings.GetSetting("physics/2d/default_gravity") * delta * Rb.GravityScale)); //Apply Gravity
			currentVelocity = currentVelocity * (float)Mathf.Clamp(1.0-Rb.LinearDamp*delta,0,1);// Apply Linear damping
			KinematicCollision2D collision = Rb.MoveAndCollide(currentVelocity * (float)delta); //Do physics tick
			if(collision != null){ // Check for collisions
				currentVelocity = currentVelocity.Bounce(collision.GetNormal()) * physicsMaterial.Bounce; //Apply Collision
			}
			GD.Print("Pos: " + Rb.GlobalPosition.ToString());
			GD.Print("Vel: " + currentVelocity.ToString());
		}
		Rb.LinearVelocity = currentVelocity;
	}
}
