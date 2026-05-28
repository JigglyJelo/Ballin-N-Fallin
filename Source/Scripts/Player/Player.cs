using Godot;
using System;

public partial class Player : Node2D{
	//Controls Variables
	public byte Id = 1; // Player #1-8
	public int Index; //To be used as a short hand instead of doing Id-1 all the time for array access
	public Color PlayerColor;
	public bool CanLaunch = true, CanSlam = true;
	public bool IsRegaining = false;
	public Vector2 InputVector, RawInputVector;
	//Children
	public CollisionShape2D RbShape;
	private CpuParticles2D flameParticles,blastParticles,popParticles;
	public AudioStreamPlayer2D RouletteSound;
	public AudioStreamPlayer2D BounceSound, FlameSound, ItemSound;
	public Trail Trail;
	private static PackedScene slamParticleScene;
	private Shadow shadow;
	//Gameplay variables
	public float LaunchPower = 0;
	public float Score;
	private Item item = null;
	public string Team = "";
	public bool FlippedStart = false, SetNewPos = true;
	private bool finished, invulnerable;
	private float playerScale = 1;
	public Vector2 SpawnPoint;
	private Emotion playerEmotion = Emotion.Neutral;
	public float StompTimer = 0;
	private bool isStomping = false;
	public bool IsStomping{
		get{return isStomping;}
		set{
			isStomping = value;
			StompTimer = 0;
		}
	}
	//Timers
	public float BounceTimer, FrozenTimer, InvulnerabilityTimer;
	private float itemRouletteTimer;
	private float textTimer = 3;
	//Vibration variables
	//Sync & Online Variables
	public int OwnerId;
	public int TicksToIgnore = 0;
	//Related and important nodes and classes
	public PlayerPhysics Physics;
	public InterpolatedBody Rb;
	public PlayerVisuals Visuals;
	public PlayerInput PlayerInput;
	public PlayerData PlayerData;

	public override void _Ready(){
		Physics = new PlayerPhysics(this);
		PlayerData = Game.PlayerDatas[Id-1];
		PlayerInput = new PlayerInput(this);
		PlayerColor = PlayerData.PlayerColor;
		Index = Id-1;
		//Nodes
		Rb = GetNode<InterpolatedBody>("RigidBody2D");
		Visuals = GetNode<PlayerVisuals>("Visuals");
		shadow = GetNode<Shadow>("Shadow");
		
		BounceSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/BounceSound");
		FlameSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/FlameSound");
		RouletteSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/RouletteSound");
		ItemSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/ItemSound");
		flameParticles = Visuals.GetNode<CpuParticles2D>("HUD/FlameParticles");
		blastParticles = GetNode<CpuParticles2D>("Particles/BlastZoneParticles");
		popParticles = GetNode<CpuParticles2D>("Particles/PopParticles");
		if(slamParticleScene == null) slamParticleScene = GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Particles/SlamParticles.tscn");
		flameParticles.ZIndex = 1;
		RbShape = GetNode<CollisionShape2D>("RigidBody2D/CollisionShape2D");

		Visuals.VisualsReady(this);
		//if(Online.IsOnline) OwnerId = Online.PlayerInfos[Id-1].UUID;
		//else OwnerId = 1;
		OwnerId = PlayerData.UUID;
		if(Game.MouseMode != Game.MouseModeEnum.Cursor) Input.MouseMode = Input.MouseModeEnum.Hidden;
		PlayerSpawned();
		Trail = GetNode<Trail>("Trail");
		Trail.PlayerRb = Rb;
		Trail.GetNode<Line2D>("TrailLine").SelfModulate = PlayerColor;
		float zoomScale = 1 + (1-Level.LevelNode.CameraZoom);
		BounceSound.MaxDistance = 2304*zoomScale;
		ItemSound.MaxDistance = 2304*zoomScale;
		FlameSound.MaxDistance = 2304*zoomScale;
		RouletteSound.MaxDistance = 2304*zoomScale;
	}

	public override void _PhysicsProcess(double delta){
		float fDelta = (float)delta;
		if(Online.IsOnline && !Mode.Finished){
			if(TicksToIgnore > 0) TicksToIgnore--;
		}
		//shadow.GlobalPosition = Rb.GlobalPosition;
		
		//Controls
		if(OwnsPlayer()){
			if(!Game.Paused && !Finished){
				if(!Finished){
					PlayerInput.DoPlayerInputs(fDelta);
				}else{
					if(Input.IsActionJustPressed("Start" + PlayerData.InputDevice) || (Game.UsingMouse() && Input.IsActionPressed("Pause Keyboard"))){
						PauseMenu.Pauser = Id;
						Mode.ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Menus/PauseMenu.tscn").Instantiate());
					}
				}
			}
		}
		Physics.DoPlayerPhysics(fDelta);
		Updates(fDelta);
		FlameCharge();
	}

	//Collisions
	public void _on_rigid_body_2d_body_entered(PhysicsBody2D body){
		Physics.OnRigidBodyEntered(body);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.SuccessfulStomp)]
	public void PlayerStomped(){
		CanLaunch = true;
		CanSlam = true;
		IsStomping = false;
	}
	//Particle effects
	public void BounceEffects(){
		BounceEffects(Visuals.GlobalPosition,Rb.LinearVelocity,0);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.BounceParticle)]
	public void BounceEffects(Vector2 position,Vector2 velocity,byte preProcessTicks){
		if(BounceTimer >= PlayerPhysics.BOUNCE_SFX_TIMEOUT){
			float velSquared = velocity.LengthSquared();
			if(velocity.Y > 100 || velocity.Y < -100){
				BounceSound.PitchScale = Game.Random.Next(80,110)/100f;
				BounceSound.Play(); 
				float lerpWeight = MathF.Sqrt(velSquared)/6000;
				PlayerInput.TriggerStrongVibration(Mathf.Lerp(0.1f,1,lerpWeight),Mathf.Lerp(0.1f,0.25f,lerpWeight));
				BounceTimer = 0;
			}
			if(velSquared > 1000*1000) ParticleManager.SpawnBounceParticle(position,velocity,this,preProcessTicks);
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.DeathParticle)]
	public void SpawnBlastParticles(Vector2 position){
		float zoomScale = 1 + (1-Level.LevelNode.CameraZoom);
		float angle = position.Angle();
		blastParticles.GlobalPosition = Level.GetEdgePosition(position,angle,3840*zoomScale,2160*zoomScale);
		blastParticles.SelfModulate = PlayerColor;
		blastParticles.Emitting = true;
		switch(Level.DetermineEdge(blastParticles.GlobalPosition,3840*zoomScale,2160*zoomScale)){
			case 0: //Right
				blastParticles.Rotation = MathF.PI;
				blastParticles.GlobalPosition += new Vector2(533.333f/Level.LevelNode.CameraZoom,0);
				break;
			case 1: //Left
				blastParticles.Rotation = 0;
				blastParticles.GlobalPosition -= new Vector2(533.333f/Level.LevelNode.CameraZoom,0);
				break;
			case 2: //Bottom
				blastParticles.Rotation = (3*MathF.PI)/2;
				blastParticles.GlobalPosition += new Vector2(0,300/Level.LevelNode.CameraZoom);
				break;
			case 3: //Top
				blastParticles.Rotation = MathF.PI/2;
				blastParticles.GlobalPosition -= new Vector2(0,300/Level.LevelNode.CameraZoom);
				break;
		}
		SFX.Play("Blast",Level.GetEdgePosition(position,angle,3840,2160));
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.PopParticle)]
	public void SpawnPopParticles(){
		popParticles.SelfModulate = PlayerColor;
		popParticles.GlobalPosition = Visuals.GlobalPosition;
		popParticles.Emitting = true;
		SFX.Play("Pop",popParticles.GlobalPosition);
	}

	public void _on_rigid_body_2d_body_exited(PhysicsBody2D body){	
		Physics.OnRigidBodyExited(body);
	}

	//Controls
	private void FlameCharge(){
		if(LaunchPower > PlayerPhysics.MAX_LAUNCH_POWER || (LaunchPower == PlayerPhysics.MAX_LAUNCH_POWER && !flameParticles.Emitting)){
			LaunchPower = PlayerPhysics.MAX_LAUNCH_POWER;
			flameParticles.Emitting = true;
			FlameSound.Play();
			if(PlayerData.VibrationEnabled && !Game.UsingMouse()){
				//START WEAK VIBRATION
				PlayerInput.SetWeakVibration(0.05f);
			}
		}else if(LaunchPower != PlayerPhysics.MAX_LAUNCH_POWER && flameParticles.Emitting){
			//STOP WEAK VIBRATION
			PlayerInput.SetWeakVibration(0);
			flameParticles.Emitting = false;
		}
	}
	public float GetChargeSpeedMultiplier(){
		float chargeSpeedMultiplier = 1;
		if(PlayerScale < 1) chargeSpeedMultiplier /= playerScale; //Small ball makes you charge faster
		chargeSpeedMultiplier *= Mode.ModeNode.GetChargeMultiplier(this);
		return chargeSpeedMultiplier;
	}

	public async void Launch(){
		if(Online.IsHost() || !Online.IsOnline){
			if(CanLaunch && InputVector != Vector2.Zero){
				Rb.Freeze = false;
				CanLaunch = false;
				Physics.ApplyLaunch();
				if(OwnsPlayer()){
					byte bAngle = (byte)(Rb.LinearVelocity.Angle()/(2*MathF.PI)*255);
					byte bPower = (byte)((LaunchPower/PlayerPhysics.MAX_LAUNCH_POWER) * 255);
					if(LaunchPower >= PlayerPhysics.MIN_VEL_FOR_LAUNCH_PARTICLE) Rpc(nameof(SpawnLaunchParticles),bAngle,bPower,Rb.GlobalPosition,0);
				}
				//Flip player sprite if necessary
				Visuals.Rpc(nameof(Visuals.Flip),InputVector.X < 0,InputVector.Angle() + (InputVector.X < 0 ? MathF.PI : 0));
				Mode.ModeNode.PlayerLaunched(this);
			}
			PlayerInput.SetWeakVibration(0);
			flameParticles.Emitting = false;
			LaunchPower = 0;
		}else if(!Mode.Finished){
			if(CanLaunch && InputVector != Vector2.Zero){
				int ping = PingGetter.GetMedianPing();
				if(Online.Buffer >= 0.5f){
					byte ticks = (byte)((1-Online.Buffer) * PingGetter.PingToTicks(ping));
					if(!Mode.Finished) RpcId(1,nameof(SendLaunchToServer),InputVector.Angle(),LaunchPower,ticks);
					if(InputVector.X < 0) Visuals.Flip(true,InputVector.Angle() + MathF.PI);
					else Visuals.Flip(false,InputVector.Angle());
					TicksToIgnore = (int)Math.Ceiling((1-Online.Buffer)*(ping / (1000.0/Engine.PhysicsTicksPerSecond)));
				}else{
					GD.Print("Ping: " + ping);
					if(!Level.IsPositionOffscreen(Rb.GlobalPosition)){
						TicksToIgnore = PingGetter.PingToTicks(ping);
						if (!Mode.Finished) RpcId(1, nameof(SendLaunchToServerNRewind), InputVector.Angle(), LaunchPower, (byte)TicksToIgnore);
						if (InputVector.X < 0) Visuals.Flip(true, InputVector.Angle() + MathF.PI);
						else Visuals.Flip(false, InputVector.Angle());
						TicksToIgnore++;
					}
				}

				if(Online.Buffer != 1){
					double timerTime = (ping / 1000.0) * Online.Buffer;
					await ToSignal(GetTree().CreateTimer(timerTime,false,true), "timeout");
					Physics.ApplyLaunch();
				}
				
				CanLaunch = false;
			}
			PlayerInput.SetWeakVibration(0);
			flameParticles.Emitting = false;
			LaunchPower = 0;
		}

		
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.LaunchParticle)]
	public void SpawnLaunchParticles(byte angle,byte magnitude,Vector2 position,byte preProcessTicks){
		ParticleManager.SpawnLaunchParticles((angle/255f)*(2*MathF.PI),(magnitude/255f) * PlayerPhysics.MAX_LAUNCH_POWER,position,preProcessTicks/(float)Engine.PhysicsTicksPerSecond);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.SlamParticle)]
	public void SpawnSlamParticle(byte preProcessTicks){
		SlamParticles slamParticle = slamParticleScene.Instantiate<SlamParticles>();
		slamParticle.GlobalPosition = Rb.GlobalPosition;
		slamParticle.Preprocess = preProcessTicks * (1/(float)Engine.PhysicsTicksPerSecond);
		AddChild(slamParticle);
	}

	public async void Slam(){
		if(Online.IsHost() || !Online.IsOnline){
			if(CanSlam){
				Rb.Freeze = false;
				CanSlam = false;
				Physics.ApplySlam();
				if(OwnsPlayer()){
					Rpc(nameof(SpawnSlamParticle),0);
				}
				Mode.ModeNode.PlayerSlammed(this);
				IsStomping = true;
			}
		}else if(CanSlam && !Mode.Finished){
			int ping = PingGetter.GetMedianPing();
			if(Online.Buffer < 0.5f){
				TicksToIgnore = PingGetter.PingToTicks(ping);
				if(!Level.IsPositionOffscreen(Rb.GlobalPosition))
					if(!Mode.Finished) RpcId(1,nameof(SendSlamToServerNRewind),(byte)TicksToIgnore);
			}else{
				byte ticks = (byte)((1-Online.Buffer) * PingGetter.PingToTicks(ping));
				if(!Mode.Finished) RpcId(1,nameof(SendSlamToServer),ticks);
			}
			double timerTime = (ping / 1000.0) * Online.Buffer;
			await ToSignal(GetTree().CreateTimer(timerTime,false,true), "timeout");
			if(Online.Buffer >= 0.5f) TicksToIgnore = (int)((1-Online.Buffer) * PingGetter.PingToTicks(ping));
			CanSlam = false;
			Physics.ApplySlam();
		}
	}

	public void ItemButtonPressed(){
		if(Item != null && !Visuals.ItemRouletteAnimation.Visible){
			RpcId(1,nameof(ClientSendUseItem));
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetItemFromEnum(byte itemEnum){
		switch((Item.ItemEnum)itemEnum){
			case Item.ItemEnum.BigFungus: Item = new BigFungus(this); break;
			case Item.ItemEnum.Booll: Item = new Booll(this); break;
			case Item.ItemEnum.BowlingBall: Item = new BowlingBall(this); break;
			case Item.ItemEnum.Inverter: Item = new Inverter(this); break;
			case Item.ItemEnum.Moon: Item = new Moon(this); break;
			case Item.ItemEnum.SmallBall: Item = new SmallBall(this); break;
			case Item.ItemEnum.Wings: Item = new Wings(this); break;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetItemFromEnum(byte itemEnum,byte amount){
		switch((Item.ItemEnum)itemEnum){
			case Item.ItemEnum.Ball: Item = new Ball(this,amount); break;
			case Item.ItemEnum.Pepper: Item = new Pepper(this,amount); break;
			case Item.ItemEnum.StopSign: Item = new StopSign(this,amount); break;
		}
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.Item)]
	private void ClientSendUseItem(){
		if(Online.IsHost()){
			if(IsRpcFromPlayerOwner()){
				if(Item is SingleUseItem suItem){
					Rpc(nameof(HostSentUseItem), (byte)Item.ItemType,suItem.Amount);
				}else if(Item != null){
					Rpc(nameof(HostSentUseItem), (byte)Item.ItemType);
				}
			}else{
				GD.PrintErr(OnlineErrorMessages.ClientSpoofErrorMessage(OwnerId));
			}
		}else{
			GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void HostSentUseItem(byte itemEnum){
		if(Item.ItemType == (Item.ItemEnum)itemEnum){
			Item.UseItem();
		}else{
			GD.PrintErr("Item desync from host");
			SetItemFromEnum(itemEnum);
			Item.UseItem();
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void HostSentUseItem(byte itemEnum, byte amount){
		if(Item.ItemType == (Item.ItemEnum)itemEnum && (Item as SingleUseItem).Amount == amount){
			Item.UseItem();
		}else{
			GD.PrintErr("Item desync from host");
			SetItemFromEnum(itemEnum,amount);
			Item.UseItem();
		}
	}

	private void Updates(float delta){

		BounceTimer += delta;

		//Timers n Stuff
		
		//Regain Checks
		Mode.ModeNode.PlayerRegainCheck(this, delta);

		//Transformation Timer and special abilities
		if(Item is TransformItem){
			TransformItem tItem = (TransformItem)Item;
			tItem.TransformItemTimer(delta);
			if(tItem.Activated){
				if(tItem is Wings) CanLaunch = true;
				else if(tItem is Moon && Physics.AirTimer >= Mode.ModeNode.MoonAirTimeRequirement){
					CanLaunch = true;
					Physics.AirTimer = 0;
				}
			}
		}

		//Invulnerability Timer
		if(Invulnerable){
			if(InvulnerabilityTimer > 0) InvulnerabilityTimer -= delta;
			else if(InvulnerabilityTimer <= 0) Invulnerable = false;
		}

		//Frozen Timer
		if(Rb.Freeze && !Finished){
			FrozenTimer += delta;
			if(FrozenTimer > 5){
				Rb.Freeze = false;
				FrozenTimer = 0;
			}
		}
	}

	private void PlayerSpawned(){
		Visuals.Flip(FlippedStart);
		Rb.SetDeferred("global_position", SpawnPoint);
		Rb.SetDeferred("linear_velocity", Vector2.Zero);
		Rb.SkipInterpolation();
		Rb.SetDeferred("global_position", SpawnPoint);
		Rb.SetDeferred("linear_velocity", Vector2.Zero);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public async void RespawnPlayer(){
		if(Online.IsHost()){
			Rb.SetDeferred("freeze",false);
			Rb.SetDeferred("global_position",SpawnPoint);
			Rb.SetDeferred("linear_velocity",Vector2.Zero);
			Rb.SetDeferred("angular_velocity",0);
			Rb.SkipInterpolation();
		}
		Vector2 ogScale = Visuals.SpritesNode.Scale;
		Visuals.SpritesNode.Scale = Vector2.Zero;
		Tween scaleTween = CreateTween();
		scaleTween.TweenProperty(Visuals.SpritesNode,"scale",ogScale,0.125f);
		CanLaunch = true;
		CanSlam = true;
		Trail.ResetTrail();
		Mode.ModeNode.PlayerRespawned(this);
		if(Online.IsHost()){ //HACK TO FIX GODOT PHYSICS WHERE PLAYER CLIPS INTO NEARBY STUFF WHEN REPSAWNING
			while(!Rb.GlobalPosition.Equals(SpawnPoint)){
				await ToSignal(GetTree().CreateTimer(0.001,false), "timeout");
				Rb.GlobalPosition = SpawnPoint;
				Rb.LinearVelocity = Vector2.Zero;
				Rb.GlobalPosition = Rb.GlobalPosition;
				Rb.LinearVelocity = Rb.LinearVelocity;
			}
			Rb.SetDeferred("global_position",SpawnPoint);
		}
	}

	public void ResetTransformation(){
		Physics.ResetPhysicsTransformations();
		Visuals.FlipV(false);
		PlayerScale = 1;

		Visuals.BallSprite.SelfModulate = new Color(Visuals.BallSprite.SelfModulate,1);
		Visuals.ShadingSprite.SelfModulate = new Color(Visuals.ShadingSprite.SelfModulate, 1);
		Visuals.OutlineSprite.SelfModulate = new Color(Visuals.OutlineSprite.SelfModulate,1);
		Physics.SetPlayerCollisionExceptions(false);
	}

	public bool OwnsPlayer(){
		if(Online.PeerIsActive()) return OwnerId == Game.GameNode.Multiplayer.GetUniqueId();
		else if(Online.IsOnlinePeer()) return OwnerId == 1;
		else return true;
	}

	//Getters n Setters
	public bool Invulnerable{
		get{return invulnerable;}
		set{
			invulnerable = value;
			Physics.SetPlayerCollisionExceptions(invulnerable);
			if(invulnerable){
				InvulnerabilityTimer = 2;
				Visuals.BallSprite.SelfModulate = new Color(Visuals.BallSprite.SelfModulate,0.5f);
				Visuals.ShadingSprite.SelfModulate = new Color(Visuals.ShadingSprite.SelfModulate,0);
			}else{
				InvulnerabilityTimer = 0;
				Visuals.BallSprite.SelfModulate = new Color(Visuals.BallSprite.SelfModulate,1);	
				Visuals.ShadingSprite.SelfModulate = new Color(Visuals.ShadingSprite.SelfModulate,1);
			}
		}
	}

	public Item Item{
		get{return item;}
		set{
			item = value;
			//Visuals.SetItemSpriteVisibility(item != null);
			Visuals.SetItemSpriteTexture();
		}
	}
	
	public bool Finished{
		get{return finished;}
		set{
			if(value){
				if(OwnsPlayer() || Online.IsHost()){
					Rpc(nameof(SyncPositionReliable),1000000f,Game.Random.Next(-100000,100000)); //8 Bytes
					Rb.SetDeferred("global_position",new Vector2(1000000f,Game.Random.Next(-100000,100000)));
					Rb.SkipInterpolation();
				}
				Trail.ResetTrail();
			}
			finished = value;
		}
	}

	public float PlayerScale{
		get{return playerScale;}
		set{
			playerScale = value;
			Visuals.HUDNode.Scale = new Vector2(playerScale,playerScale);
			Physics.UpdateRadius();
			Visuals.ResetPlayerScale();
		}
	}

	public enum Emotion{
		Happy, Sad, Angry, Annoyed, Shocked, Bumped, Neutral
	}

	public Emotion PlayerEmotion{
		get{return playerEmotion;}
		set{
			playerEmotion = value;
		}
	}

	//Online & Networking
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void PlayerOffscreenOnHost(){
		if(TicksToIgnore != 0){
			Rb.GlobalPosition = new Vector2(100000,-100000);
			Rb.SkipInterpolation();
	   		Rb.LinearVelocity = Vector2.Zero;
			Rb.AngularVelocity = 0;
		}
	}

	public bool IsRpcFromResponsibleDeviceWPrediction(){
		return Online.IsRpcFromHost() || (IsRpcFromPlayerOwner() && Online.Buffer != 1);
	}
	//Sync Variable Functions
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SyncPositionReliable(float xPos,float yPos){
		if(!Online.IsHost() && Online.IsRpcFromHost() && TicksToIgnore == 0){
			Rb.GlobalPosition = new Vector2(xPos,yPos);
			Rb.SkipInterpolation();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.SendLaunch)]
	private void SendLaunchToServerNRewind(float angle,float launchPower,byte ticks){
		if(Online.IsHost() && IsRpcFromPlayerOwner() && Online.Buffer == 0 && !Level.IsPositionOffscreenOrDead(Rb.GlobalPosition)){
			//Get old position
			Vector2 rewindedPosition = Trail.GetPreviousPosition(ticks);
			if(Level.IsPositionOffscreenOrDead(rewindedPosition)) return;
			Trail.SlicePoints(ticks);
			Rb.GlobalPosition = rewindedPosition;
			Rb.SkipInterpolation();
			InputVector = Vector2.FromAngle(angle);
			if (launchPower > PlayerPhysics.MAX_LAUNCH_POWER) launchPower = PlayerPhysics.MAX_LAUNCH_POWER;
			else if (launchPower < 0) launchPower = 0;
			LaunchPower = launchPower;
			if(!CanLaunch){
				//GD.PrintErr(Online.PlayerInfos[Id-1] + " launched when they shouldnt");
				GD.PrintErr(OwnerId + " launched when they shouldnt");
				CanLaunch = true;
			}
			if(CanLaunch){
				Launch();
				byte bAngle = (byte)(angle/(2*MathF.PI) *255);
				byte bPower = (byte)((launchPower / PlayerPhysics.MAX_LAUNCH_POWER) * 255);
				if(LaunchPower >= PlayerPhysics.MIN_VEL_FOR_LAUNCH_PARTICLE) Rpc(nameof(SpawnLaunchParticles),bAngle,bPower,Rb.GlobalPosition,ticks);
				Online.PredictPosition(Rb,ticks);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.SendLaunch)]
	private void SendLaunchToServer(float angle,float launchPower,byte ticks){
		if(Online.IsHost() && IsRpcFromPlayerOwner()){
			InputVector = Vector2.FromAngle(angle);
			if (launchPower > PlayerPhysics.MAX_LAUNCH_POWER) launchPower = PlayerPhysics.MAX_LAUNCH_POWER;
			else if (launchPower < 0) launchPower = 0;
			LaunchPower = launchPower;
			if(!CanLaunch){
				//GD.PrintErr(Online.PlayerInfos[Id-1] + " launched when they shouldnt");
				GD.PrintErr(OwnerId + " launched when they shouldnt");
				CanLaunch = true;
			}
			if(CanLaunch){
				Launch();
				byte bAngle = (byte)(angle/(2*MathF.PI) *255);
				byte bPower = (byte)((launchPower / PlayerPhysics.MAX_LAUNCH_POWER) * 255);
				if(LaunchPower >= PlayerPhysics.MIN_VEL_FOR_LAUNCH_PARTICLE) Rpc(nameof(SpawnLaunchParticles),bAngle,bPower,Rb.GlobalPosition,ticks);
				Online.PredictPosition(Rb,ticks);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.SendSlam)]
	private void SendSlamToServerNRewind(byte ticks){
		if(Online.IsHost() && IsRpcFromPlayerOwner() && Online.Buffer == 0  && !Level.IsPositionOffscreenOrDead(Rb.GlobalPosition)){
			//Get old position
			Vector2 rewindedPosition = Trail.GetPreviousPosition(ticks);
			if(Level.IsPositionOffscreenOrDead(rewindedPosition)) return;
			Trail.SlicePoints(ticks);
			Rb.GlobalPosition = rewindedPosition;
			Rb.SkipInterpolation();
			//Slam
			CanSlam = true;
			if(CanSlam){
				Slam();
				Rpc(nameof(SpawnSlamParticle),ticks);
				Online.PredictPosition(Rb,ticks);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.SendSlam)]
	private void SendSlamToServer(byte ticks){
		if(Online.IsHost() && IsRpcFromPlayerOwner()){
			CanSlam = true;
			if(CanSlam){
				Slam();
				Rpc(nameof(SpawnSlamParticle),ticks);
				Online.PredictPosition(Rb,ticks);
			}
		}
	}

	public bool IsRpcFromPlayerOwner(){
		return OwnerId == GetTree().GetMultiplayer().GetRemoteSenderId();
	}
}