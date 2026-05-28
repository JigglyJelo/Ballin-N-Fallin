using Godot;
using System;

public partial class Player : Node2D{
	//Constants
	public const float MAX_LAUNCH_TIME = 1.25f; // Amount of time it takes to fully charge
	public const float MAX_LAUNCH_POWER = 4000; // The maximum amount of power added onto a launch based on charge time (Plus the MIN_LAUNCH_POWER)
	public const float MIN_LAUNCH_POWER = 500; // Amount of power always added onto a launch no matter time held
	public const float SLAM_POWER = 2000;
	public const float GRAVITY = 2;
	public const float MASS = 1;
	public const float LINEAR_DAMP = 0.2f;
	public const float ANGULAR_DAMP = 0.125f;
	public const float FRICTION = 1;
	public const float BOUNCE = 0.65f; // The bounciness of the player
	public const float RADIUS = 91; // The radius of the player
	public const float SPEED_CAP = 10000; //The maximum velocity in any direction
	private const float MIN_STRETCH_SPEED = 2300; //The minimum velocity before the squash n stretch effect starts
	private const float BOUNCE_SFX_TIMEOUT = 0.15f;
	private const float MIN_STOMP_SPEED = (float)(SLAM_POWER*0.5); //Minimum velocity downwards you must be going to stay in stomping state
	private const float MIN_VEL_FOR_LAUNCH_PARTICLE = (float)(MAX_LAUNCH_POWER * 0.15); //The amount your launch needed to be charged for particle to spawn
	//Controls Variables
	public byte Id = 1; // Player #1-8
	public int Index; //To be used as a short hand instead of doing Id-1 all the time for array access
	public Color PlayerColor;
	public bool CanLaunch = true, CanSlam = true;
	public bool IsRegaining = false;
	public Vector2 InputVector, RawInputVector;
	//Children
	public PlayerVisuals Visuals;
	public InterpolatedBody Rb;
	public CollisionShape2D RbShape;
	private CpuParticles2D flameParticles,blastParticles,popParticles;
	public AudioStreamPlayer2D RouletteSound;
	public AudioStreamPlayer2D BounceSound, FlameSound, ItemSound;
	public Trail Trail;
	private static PackedScene slamParticleScene;
	
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
	private float stompTimer = 0;
	private bool isStomping = false;
	public bool IsStomping{
		get{return isStomping;}
		set{
			isStomping = value;
			stompTimer = 0;
		}
	}
	//Timers
	public float BounceTimer, FrozenTimer, StillTimer, AirTimer, InvulnerabilityTimer;
	private float itemRouletteTimer, vibrationTimer;
	private float textTimer = 3;
	//Vibration variables
	private float strongVibration, weakVibration;
	//Sync & Online Variables
	public int OwnerId;
	public int TicksToIgnore = 0;
	private Shadow shadow;
	//Input Manager
	public PlayerInput PlayerInput;
	public PlayerData PlayerData;

	public override void _Ready(){
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
		float velocityMagnitudeSquared = Rb.LinearVelocity.LengthSquared();
		if(Online.IsHost()){
			if((Rb.GlobalPosition.Y>2500/Level.LevelNode.CameraZoom || (Rb.GlobalPosition.Y<-2500/Level.LevelNode.CameraZoom && Rb.GlobalPosition.Y>-10000/Level.LevelNode.CameraZoom) || (Rb.GlobalPosition.X<-4444/Level.LevelNode.CameraZoom && Rb.GlobalPosition.X>-17777/Level.LevelNode.CameraZoom) || (Rb.GlobalPosition.X>4444/Level.LevelNode.CameraZoom && Rb.GlobalPosition.X<17777/Level.LevelNode.CameraZoom)) && !Finished) 
				Death.KillPlayer(this,Death.DeathCause.Pop);//RespawnPlayer(); //Incase you clip oob
			if(velocityMagnitudeSquared > SPEED_CAP*SPEED_CAP){ //Speed cap
				float angle = Rb.LinearVelocity.Angle();
				Rb.LinearVelocity = Vector2.FromAngle(angle) * SPEED_CAP;
			}
		}
		
		if(velocityMagnitudeSquared >= (MIN_STRETCH_SPEED*MIN_STRETCH_SPEED)){//&& !isRegaining
			Visuals.SquashNStretch(MathF.Sqrt(velocityMagnitudeSquared));
			if(Rb.AngularDamp != 25 && (velocityMagnitudeSquared >= (MIN_STRETCH_SPEED*2 * MIN_STRETCH_SPEED*2) || Rb.AngularVelocity > 5 || Rb.AngularVelocity < -5)){
				Rb.AngularDamp = 25;
			}else{
				Rb.AngularDamp = Mathf.Lerp(Rb.AngularDamp,ANGULAR_DAMP,0.5f);
			}
		}else{
			//float lerpScale = 1-(velocityMagnitudeSquared / ((MIN_STRETCH_SPEED*MIN_STRETCH_SPEED)));
			Rb.AngularDamp = Mathf.Lerp(Rb.AngularDamp,ANGULAR_DAMP,0.125f);
			Visuals.ResetSquashNStretch();
		}
		
		Updates(fDelta);
		FlameCharge();
	}

	//Collisions
	public void _on_rigid_body_2d_body_entered(PhysicsBody2D body){
		if(body.IsInGroup("NoRegain")){
			BounceEffects();
			if(body.IsInGroup("Bump")) PlayerEmotion = Emotion.Bumped;
			if(IsStomping && Rb.LinearVelocity.Y <= MIN_STOMP_SPEED){
				//IsStomping = false;
			}
		}else if(body.IsInGroup("Regain") || body.GetParent().IsInGroup("Regain")){
			if(!IsRegaining) BounceEffects();
			IsRegaining = true;
			Mode.ModeNode.OnPlayerEnterRegain(this);
			StillTimer = 0;
			AirTimer = 0;
			if(IsStomping && Rb.LinearVelocity.Y <= MIN_STOMP_SPEED){
				//IsStomping = false;
			}
		}else if(body.IsInGroup("Bump")){
			PlayerEmotion = Emotion.Bumped;
			if(IsStomping && Rb.LinearVelocity.Y <= MIN_STOMP_SPEED){
				//IsStomping = false;
			}
		}
		if(body.GetParent().IsInGroup("Player")){
			Player otherPlayer = body.GetParent() as Player;
			if(isSuccessfulStomp()){ //Checks whether the player is being stomped on
				if(Online.IsHost()){
					//Stomp is successful
					Mode.ModeNode.PlayerKilledPlayer(this, otherPlayer, Death.DeathCause.Stomp);
					Death.KillPlayer(this,Death.DeathCause.Stomp);
					otherPlayer.Rpc(nameof(PlayerStomped));
					otherPlayer.Rb.LinearVelocity = new Vector2(otherPlayer.Rb.LinearVelocity.X,-2000f);
				}
			}else{
				if(Online.IsHost()) Mode.ModeNode.PlayerBumpedPlayer(otherPlayer,this);
				PlayerEmotion = Emotion.Bumped;
        		otherPlayer.PlayerEmotion = Emotion.Bumped;
			}

			bool isSuccessfulStomp(){
				return Game.StompSetting != Game.StompSettingEnum.Off && otherPlayer.IsStomping && //Make sure player is slaming
				Rb.GlobalPosition.Y >= otherPlayer.Rb.GlobalPosition.Y && //Stomping player above stomped player
				MathF.Abs(Rb.GlobalPosition.X-otherPlayer.Rb.GlobalPosition.X) <= 150f && //Stomping player is horizontally aligned with stomper
				PlayerScale <= otherPlayer.PlayerScale && //Stomping player is bigger than or equal to stomped scale
				!Level.IsPositionOffscreenOrDead(Rb.GlobalPosition) && //Can't stomp dead (or i guess offscreen) players
				(Team.Equals("") || ((!Team.Equals("") && !Team.Equals(otherPlayer.Team)) || Game.StompSetting == Game.StompSettingEnum.TeamAttack)); //Stomping player is on another team or there are no teams
			}

			//Unfreeze frozen players on collision
			if(otherPlayer.Rb.Freeze && !otherPlayer.Finished){
				otherPlayer.Rb.SetDeferred("freeze",false);
				GD.Print("Unfrozen");
			}
			Vibration();
		}
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
		if(BounceTimer >= BOUNCE_SFX_TIMEOUT){
			float velSquared = velocity.LengthSquared();
			if(velocity.Y > 100 || velocity.Y < -100){
				BounceSound.PitchScale = Game.Random.Next(80,110)/100f;
				BounceSound.Play(); 
				float lerpWeight = MathF.Sqrt(velSquared)/6000;
				strongVibration = Mathf.Lerp(0.1f,1,lerpWeight);
				vibrationTimer = Mathf.Lerp(0.1f,0.25f,lerpWeight);
				Vibration();
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
		if(body.IsInGroup("Regain") || body.GetParent().IsInGroup("Regain")){
			IsRegaining = false;
		}
	}

	//Controls
	private void FlameCharge(){
		if(LaunchPower > MAX_LAUNCH_POWER || (LaunchPower == MAX_LAUNCH_POWER && !flameParticles.Emitting)){
			LaunchPower = MAX_LAUNCH_POWER;
			flameParticles.Emitting = true;
			FlameSound.Play();
			if(PlayerData.VibrationEnabled && !Game.UsingMouse()){
				//START WEAK VIBRATION
				weakVibration = 0.05f;
				Vibration();
			}
		}else if(LaunchPower != MAX_LAUNCH_POWER && flameParticles.Emitting){
			//STOP WEAK VIBRATION
			weakVibration = 0;
			Vibration();
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
				addVelocity();
				if(OwnsPlayer()){
					byte bAngle = (byte)(Rb.LinearVelocity.Angle()/(2*MathF.PI)*255);
					byte bPower = (byte)((LaunchPower/MAX_LAUNCH_POWER) * 255);
					if(LaunchPower >= MIN_VEL_FOR_LAUNCH_PARTICLE) Rpc(nameof(SpawnLaunchParticles),bAngle,bPower,Rb.GlobalPosition,0);
				}
				//Flip player sprite if necessary
				Visuals.Rpc(nameof(Visuals.Flip),InputVector.X < 0,InputVector.Angle() + (InputVector.X < 0 ? MathF.PI : 0));
				Mode.ModeNode.PlayerLaunched(this);
			}
			weakVibration = 0;
			Vibration();
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
					addVelocity();
				}
				
				CanLaunch = false;
			}
			weakVibration = 0;
			Vibration();
			flameParticles.Emitting = false;
			LaunchPower = 0;
		}

		void addVelocity(){
		    // If current velocity is opposite to launch direction, reduce velocity
		    float diff = Rb.LinearVelocity.AngleTo(InputVector);

		    //Normalize the angle difference to range -π to π
		    diff = MathF.Atan2(MathF.Sin(diff), MathF.Cos(diff));

		    if(MathF.Abs(diff) >= (float)(Math.PI/4)){
		        Rb.LinearVelocity *= 0.5f; // Less aggressive reduction
		    }

		    //Apply launch force
			//Rb.ApplyImpulse(InputVector * (LaunchPower + MIN_LAUNCH_POWER));
		    Rb.LinearVelocity += InputVector * (LaunchPower + MIN_LAUNCH_POWER);
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.LaunchParticle)]
	public void SpawnLaunchParticles(byte angle,byte magnitude,Vector2 position,byte preProcessTicks){
		ParticleManager.SpawnLaunchParticles((angle/255f)*(2*MathF.PI),(magnitude/255f) * MAX_LAUNCH_POWER,position,preProcessTicks/(float)Engine.PhysicsTicksPerSecond);
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
				if(Rb.LinearVelocity.Y < 0) Rb.LinearVelocity = new Vector2(Rb.LinearVelocity.X, Rb.LinearVelocity.Y * 0.5f);
				Rb.LinearVelocity += Vector2.Down * SLAM_POWER * (Rb.GravityScale == 0 ? 1 : Rb.GravityScale/GRAVITY);
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
			if(Rb.LinearVelocity.Y < 0) Rb.LinearVelocity = new Vector2(Rb.LinearVelocity.X, Rb.LinearVelocity.Y * 0.5f);
			Rb.LinearVelocity += Vector2.Down * SLAM_POWER * (Rb.GravityScale == 0 ? 1 : Rb.GravityScale/GRAVITY);
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

		//Gives player launch and slam back if stuck in air
		if(!CanLaunch || !CanSlam){
			if(AirTimer <= 10) AirTimer += delta;
			else{
				CanSlam = true;
				CanLaunch = true;
				AirTimer = 0;
			}
		}

		//Transformation Timer and special abilities
		if(Item is TransformItem){
			TransformItem tItem = (TransformItem)Item;
			tItem.TransformItemTimer(delta);
			if(tItem.Activated){
				if(tItem is Wings) CanLaunch = true;
				else if(tItem is Moon && AirTimer >= Mode.ModeNode.MoonAirTimeRequirement){
					CanLaunch = true;
					AirTimer = 0;
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

		if(vibrationTimer > 0) vibrationTimer -= delta;
		else if(vibrationTimer < 0){
			vibrationTimer = 0;
			strongVibration = 0;
			Vibration();
		}
		if(IsStomping){
			stompTimer += delta;
			const float MIN_STOMP_TIME = 0.25f;
			if(Rb.LinearVelocity.Y <= MIN_STOMP_SPEED && stompTimer >= MIN_STOMP_TIME){
				IsStomping = false;
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

	
	//Does Vibration only if enabled and not already vibrating from charge as it would get overwritten
	public void Vibration(){
		if(!Mode.Finished && PlayerData.VibrationEnabled && !Game.UsingMouse() && OwnsPlayer()){
			if(weakVibration == 0 && strongVibration == 0) Input.StopJoyVibration((int)PlayerData.InputDevice);
			else Input.StartJoyVibration((int)PlayerData.InputDevice, weakVibration, strongVibration);
		}
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
		Rb.GravityScale = GRAVITY;
		Rb.Mass = MASS;
		Rb.LinearDamp = LINEAR_DAMP;
		Rb.AngularDamp = ANGULAR_DAMP;
		PhysicsMaterial physicsMaterial = new PhysicsMaterial();
		physicsMaterial.Friction = FRICTION;
		physicsMaterial.Bounce = BOUNCE;
		Rb.PhysicsMaterialOverride = physicsMaterial;
		Visuals.FlipV(false);
		PlayerScale = 1;

		Visuals.BallSprite.SelfModulate = new Color(Visuals.BallSprite.SelfModulate,1);
		Visuals.ShadingSprite.SelfModulate = new Color(Visuals.ShadingSprite.SelfModulate, 1);
		Visuals.OutlineSprite.SelfModulate = new Color(Visuals.OutlineSprite.SelfModulate,1);
		foreach(Player player in Game.Players) if(player != null) Rb.RemoveCollisionExceptionWith(player.Rb);
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
			if(invulnerable){
				foreach(Player player in Game.Players) Rb.AddCollisionExceptionWith(player.Rb);
				InvulnerabilityTimer = 2;
				Visuals.BallSprite.SelfModulate = new Color(Visuals.BallSprite.SelfModulate,0.5f);
				Visuals.ShadingSprite.SelfModulate = new Color(Visuals.ShadingSprite.SelfModulate,0);
			}else{
				InvulnerabilityTimer = 0;
				foreach(Player player in Game.Players) Rb.RemoveCollisionExceptionWith(player.Rb);
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
			CircleShape2D defaultCircle = new CircleShape2D();
			defaultCircle.Radius = RADIUS * playerScale;
			RbShape.Shape = defaultCircle;
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
			if(launchPower > MAX_LAUNCH_POWER + MIN_LAUNCH_POWER) launchPower = MAX_LAUNCH_POWER + MIN_LAUNCH_POWER;
			else if(launchPower < MIN_LAUNCH_POWER) launchPower = MIN_LAUNCH_POWER;
			LaunchPower = launchPower;
			if(!CanLaunch){
				//GD.PrintErr(Online.PlayerInfos[Id-1] + " launched when they shouldnt");
				GD.PrintErr(OwnerId + " launched when they shouldnt");
				CanLaunch = true;
			}
			if(CanLaunch){
				Launch();
				byte bAngle = (byte)(angle/(2*MathF.PI) *255);
				byte bPower = (byte)(((launchPower-MIN_LAUNCH_POWER)/MAX_LAUNCH_POWER) * 255);
				if(LaunchPower >= MIN_VEL_FOR_LAUNCH_PARTICLE) Rpc(nameof(SpawnLaunchParticles),bAngle,bPower,Rb.GlobalPosition,ticks);
				Online.PredictPosition(Rb,ticks);
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.SendLaunch)]
	private void SendLaunchToServer(float angle,float launchPower,byte ticks){
		if(Online.IsHost() && IsRpcFromPlayerOwner()){
			InputVector = Vector2.FromAngle(angle);
			if(launchPower > MAX_LAUNCH_POWER + MIN_LAUNCH_POWER) launchPower = MAX_LAUNCH_POWER + MIN_LAUNCH_POWER;
			else if(launchPower < MIN_LAUNCH_POWER) launchPower = MIN_LAUNCH_POWER;
			LaunchPower = launchPower;
			if(!CanLaunch){
				//GD.PrintErr(Online.PlayerInfos[Id-1] + " launched when they shouldnt");
				GD.PrintErr(OwnerId + " launched when they shouldnt");
				CanLaunch = true;
			}
			if(CanLaunch){
				Launch();
				byte bAngle = (byte)(angle/(2*MathF.PI) *255);
				byte bPower = (byte)(((launchPower-MIN_LAUNCH_POWER)/MAX_LAUNCH_POWER) * 255);
				if(LaunchPower >= MIN_VEL_FOR_LAUNCH_PARTICLE) Rpc(nameof(SpawnLaunchParticles),bAngle,bPower,Rb.GlobalPosition,ticks);
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