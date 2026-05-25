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
	public const int DEFAULT_TEXTURE_SIZE = 190; //The size of the default resolution (2160p) player sprites
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
	private bool isRegaining = false;
	public Vector2 InputVector, RawInputVector;
	//Children
	private Sprite2D itemSprite, arrowSprite;
	public Sprite2D BallSprite, LinesSprite, OutlineSprite, EyesSprite, PupilsSprite, ShadingSprite;
	public AnimatedSprite2D ItemRouletteAnimation;
	public static Texture2D BasketBallTexture, LinesTexture,OutlineTexture;
	private static Texture2D neutralEye, neutralPupil,happyEye,sadEye,angryEye,annoyedEye,shockedEye,shockedPupil,bumpedEye;
	private static Texture2D bigLinesTexture, bigNeutralEye, bigNeutralPupil,bigHappyEye,bigSadEye,bigAngryEye,bigAnnoyedEye,bigShockedEye,bigShockedPupil,bigBumpedEye;
	public static Texture2D BigBasketBall, BigOutlineTexture;
	public static float TextureScale = 1;
	public static int BallSize;
	private static int bigBallSize;
	private float playerScale = 1;
	public Label ItemAmountText;
	private Label playerText, usernameText;
	private CanvasGroup usernameGroup;
	public InterpolatedBody Rb;
	public CollisionShape2D RbShape;
	public TextureProgressBar TransformBar;
	public Node2D VisualsNode, SpritesNode, RotationsNode, HUDNode;
	private CpuParticles2D flameParticles,blastParticles,popParticles;
	public AudioStreamPlayer2D RouletteSound;
	private AudioStreamPlayer2D bounceSound, flameSound, itemSound;
	public Trail Trail;
	private static PackedScene slamParticleScene;
	private Polygon2D itemTriangle;
	//Gameplay variables
	public float LaunchPower = 0;
	public float Score;
	private Item item = null;
	public string Team = "";
	public bool FlippedStart = false;
	private bool finished, invulnerable, setNewPos = true;
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
	public float BounceTimer, FrozenTimer;
	private float itemRouletteTimer, stillTimer, airTimer, invulnerabilityTimer, emotionTimer, vibrationTimer;
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
		VisualsNode = GetNode<Node2D>("Visuals");
		shadow = GetNode<Shadow>("Shadow");
		SpritesNode = VisualsNode.GetNode<Node2D>("Sprites");
		RotationsNode = SpritesNode.GetNode<Node2D>("Rotation");
		HUDNode = VisualsNode.GetNode<Node2D>("HUD");
		ShadingSprite = RotationsNode.GetNode<Sprite2D>("Shading");
		playerText = HUDNode.GetNode<Label>("ModeText");
		usernameText = HUDNode.GetNode<Label>("UsernameGroup/UsernameText");
		usernameGroup = HUDNode.GetNode<CanvasGroup>("UsernameGroup");
		BallSprite = RotationsNode.GetNode<Sprite2D>("BallSprite");
		LinesSprite = RotationsNode.GetNode<Sprite2D>("LinesSprite");
		OutlineSprite = RotationsNode.GetNode<Sprite2D>("OutlineSprite");
		EyesSprite = LinesSprite.GetNode<Sprite2D>("EyesSprite");
		PupilsSprite = EyesSprite.GetNode<Sprite2D>("PupilsSprite");
		arrowSprite = HUDNode.GetNode<Sprite2D>("ArrowSprite");
		itemSprite = HUDNode.GetNode<Sprite2D>("ItemSprite");
		ItemAmountText = itemSprite.GetNode<Label>("ItemAmountLabel");
		itemTriangle = HUDNode.GetNode<Polygon2D>("ItemTriangle");
		TransformBar = HUDNode.GetNode<TextureProgressBar>("Item Bar");
		ItemRouletteAnimation = HUDNode.GetNode<AnimatedSprite2D>("RouletteAnimation");
		ItemRouletteAnimation.Pause();
		ItemRouletteAnimation.Visible = false;
		HUDNode.TopLevel = true;
		bounceSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/BounceSound");
		flameSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/FlameSound");
		RouletteSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/RouletteSound");
		itemSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/ItemSound");
		flameParticles = VisualsNode.GetNode<CpuParticles2D>("HUD/FlameParticles");
		blastParticles = GetNode<CpuParticles2D>("Particles/BlastZoneParticles");
		popParticles = GetNode<CpuParticles2D>("Particles/PopParticles");
		if(slamParticleScene == null) slamParticleScene = GD.Load<PackedScene>("res://Scenes/Object Scenes/Particles/SlamParticles.tscn");
		flameParticles.ZIndex = 1;
		RbShape = GetNode<CollisionShape2D>("RigidBody2D/CollisionShape2D");

		if(Game.Resolution >= 4320) BallSize = DEFAULT_TEXTURE_SIZE * 2;
		else if(Game.Resolution >= 2160) BallSize = DEFAULT_TEXTURE_SIZE;
		else if(Game.Resolution >= 1440) BallSize = 127; //Math.Round(DEFAULT_TEXTURE_SIZE / 1.5f);
		else if(Game.Resolution >= 1080) BallSize = 95; //Math.Round(DEFAULT_TEXTURE_SIZE / 2f);
		else BallSize = 63; //Math.Round(DEFAULT_TEXTURE_SIZE / 3f);
		TextureScale = (float)DEFAULT_TEXTURE_SIZE / BallSize;
		if(LinesTexture == null || LinesTexture.GetSize().Y != BallSize){
			BasketBallTexture = GD.Load<Texture2D>("res://Assets/Sprites/Player/Ball" + (BallSize >= 380 ? 63 : 31) + ".png");
			neutralEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/NeutralEyes.png");
			neutralPupil = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Pupils/NeutralPupils.png");
			happyEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/HappyEyes.png");
			sadEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/SadEyes.png");
			angryEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/AngryEyes.png");
			annoyedEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/AnnoyedEyes.png");
			shockedEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/ShockedEyes.png");
			shockedPupil = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Pupils/ShockedPupils.png");
			bumpedEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Eyes/BumpedEyes.png");
			OutlineTexture = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/Outline.png");
			LinesTexture = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + BallSize + "/BallLines.png");
		}
		if(Game.Resolution >= 4320) bigBallSize = 500;
		else if(Game.Resolution >= 2160) bigBallSize = 380;
		else if(Game.Resolution >= 1440) bigBallSize = 380;
		else if(Game.Resolution >= 1080) bigBallSize = 190;
		else if(Game.Resolution >= 720) bigBallSize = 127;
		else if(Game.Resolution >= 486) bigBallSize = 95;
		else bigBallSize = 63;
		if(bigLinesTexture == null|| bigLinesTexture.GetSize().Y != bigBallSize){
			BigBasketBall = GD.Load<Texture2D>("res://Assets/Sprites/Player/Ball" + (bigBallSize >= 380 ? 63 : 31) + ".png");
			bigNeutralEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/NeutralEyes.png");
			bigNeutralPupil = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Pupils/NeutralPupils.png");
			bigHappyEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/HappyEyes.png");
			bigSadEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/SadEyes.png");
			bigAngryEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/AngryEyes.png");
			bigAnnoyedEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/AnnoyedEyes.png");
			bigShockedEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/ShockedEyes.png");
			bigShockedPupil = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Pupils/ShockedPupils.png");
			bigBumpedEye = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Eyes/BumpedEyes.png");
			BigOutlineTexture = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/Outline.png");
			bigLinesTexture = GD.Load<Texture2D>("res://Assets/Sprites/Player/" + bigBallSize + "/BallLines.png");
		}	
		
		if(BallSize != DEFAULT_TEXTURE_SIZE){
			float ballSpriteScale = (BallSize/(float)(BasketBallTexture.GetHeight()*2)) * TextureScale;
			Vector2 textureScale = new Vector2(TextureScale,TextureScale);
			LinesSprite.Scale = textureScale;
			BallSprite.Texture = BasketBallTexture;
			BallSprite.Scale = new Vector2(ballSpriteScale,ballSpriteScale);
			BallSprite.RegionRect = new Rect2(0,0,BallSprite.Texture.GetWidth()*2,BallSprite.Texture.GetHeight()*2);
			EyesSprite.Texture = neutralEye;
			PupilsSprite.Texture = neutralPupil;
			OutlineSprite.Texture = OutlineTexture;
			LinesSprite.Texture = LinesTexture;
			OutlineSprite.Scale = textureScale;
		}
		
		BallSprite.SelfModulate = PlayerColor;
		arrowSprite.SelfModulate = PlayerColor;
		playerText.SelfModulate = PlayerColor;
		TransformBar.SelfModulate = PlayerColor;
		itemTriangle.Color = PlayerColor;
		ShadingSprite.SelfModulate = PlayerColor;
		arrowSprite.ZIndex = 1;
		if(Game.TotalPlayers != 2){
			switch(Team){
				case "A": OutlineSprite.SelfModulate = Game.TeamColors[0]; arrowSprite.SelfModulate = Game.TeamColors[0]; break;
				case "B": OutlineSprite.SelfModulate = Game.TeamColors[1]; arrowSprite.SelfModulate = Game.TeamColors[1]; break;
				case "C": OutlineSprite.SelfModulate = Game.TeamColors[2]; arrowSprite.SelfModulate = Game.TeamColors[2]; break;
				case "D":  OutlineSprite.SelfModulate = Game.TeamColors[3]; arrowSprite.SelfModulate = Game.TeamColors[3]; break;
			}
		}
		//if(Online.IsOnline) OwnerId = Online.PlayerInfos[Id-1].UUID;
		//else OwnerId = 1;
		OwnerId = PlayerData.UUID;
		if(Game.MouseMode != Game.MouseModeEnum.Cursor) Input.MouseMode = Input.MouseModeEnum.Hidden;
		PlayerSpawned();
		Trail = GetNode<Trail>("Trail");
		Trail.PlayerRb = Rb;
		Trail.GetNode<Line2D>("TrailLine").SelfModulate = PlayerColor;
		float zoomScale = 1 + (1-Level.LevelNode.CameraZoom);
		bounceSound.MaxDistance = 2304*zoomScale;
		itemSound.MaxDistance = 2304*zoomScale;
		flameSound.MaxDistance = 2304*zoomScale;
		RouletteSound.MaxDistance = 2304*zoomScale;
		usernameText.Text = Game.GetUsername(Id);
		ShowUsernameText();
		if(!AccessibilityMenu.AlwaysShowNames){
			Tween usernameTween = CreateTween();
			usernameTween.TweenProperty(usernameGroup,"self_modulate",new Color(PlayerColor,0),3);
			usernameTween.TweenCallback(Callable.From(HideUsernameText));
		}
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
						Mode.ModeNode.AddChild(GD.Load<PackedScene>("res://Scenes/Object Scenes/Menus/PauseMenu.tscn").Instantiate());
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
			SquashNStretch(MathF.Sqrt(velocityMagnitudeSquared));
			if(Rb.AngularDamp != 25 && (velocityMagnitudeSquared >= (MIN_STRETCH_SPEED*2 * MIN_STRETCH_SPEED*2) || Rb.AngularVelocity > 5 || Rb.AngularVelocity < -5)){
				Rb.AngularDamp = 25;
			}else{
				Rb.AngularDamp = Mathf.Lerp(Rb.AngularDamp,ANGULAR_DAMP,0.5f);
			}
		}else{
			//float lerpScale = 1-(velocityMagnitudeSquared / ((MIN_STRETCH_SPEED*MIN_STRETCH_SPEED)));
			Rb.AngularDamp = Mathf.Lerp(Rb.AngularDamp,ANGULAR_DAMP,0.125f);
			SpritesNode.Scale = SpritesNode.Scale.Lerp(Vector2.One,0.125f);
			SpritesNode.Skew = Mathf.Lerp(SpritesNode.Skew,0,0.125f);
		}
		
		Updates(fDelta);
		FlameCharge();
	}

	public override void _Process(double delta){
		EyeAndArrowUpdate((float)delta);
		ShadingSprite.GlobalRotation = 0;
    }

    private void SquashNStretch(float velocityMagnitude){
		const float LERP_AMOUNT = 0.125f;
		//Modify scale and skew based off velocity and rotation to give illusion of stretching in direction of velocity
		float angle = Rb.ToLocal(Rb.GlobalPosition + Rb.LinearVelocity).Angle();
		const float TWO_PI = (float)(2*Math.PI);
		angle = angle % TWO_PI; // Ensure angle is within [0, 2π)
		if(angle < 0) angle += TWO_PI; // Adjust negative angles
		angle = TWO_PI - angle; //Convert angle from clockwise unit circle to counterclockwise unit circle
		//If sign in quadrant 1 or 3 sign is positive if sign in quadrant 2 or 4 its negative
		const float HALF_PI = (float)(Math.PI / 2);
		int quadrant = (int)(angle / HALF_PI) + 1; //Get quadrant of angle
		//int quadrant = ((int)(angle / HALF_PI) & 3) + 1; //Addresses the possibility of getting quadrant 5
		//if(quadrant == 5) GD.Print("Quadrant 5 lol");
		//int sign = quadrant % 2 == 1 ? 1 : -1;
		int sign = 2 * (quadrant & 1) - 1; // Odd Positive Even Negative
		float convertedAngle = angle % HALF_PI; // Cut angle into first quarter of unit circle [0,π/2)
		const float Q1_MIDPOINT = (float)(Math.PI / 4);
		float difference = MathF.Abs(convertedAngle-Q1_MIDPOINT); //Get the difference between cut angle and midpoint of first quarter to get dif [0,π/4]
		//Skew
		float skewAngle = MathF.Abs(difference-Q1_MIDPOINT) * sign;//Get the angle to skew at by subtracting our dif from the midpoint and setting its sign//((difference/(MathF.PI / 4))*MathF.PI/2) * sign;
		SpritesNode.GlobalSkew = Mathf.Lerp(SpritesNode.GlobalSkew,skewAngle,LERP_AMOUNT);
		//Scaling
		float scale = 1-((difference / Q1_MIDPOINT) * 0.5f);///Set the scale of whichever axis will be squished by the dif / by the midpoint * the multiplier
		float speedScale = (velocityMagnitude-MIN_STRETCH_SPEED) / (8000-MIN_STRETCH_SPEED); //Slower speed equals less squished faster speed equals more squished
		if(speedScale > 1) speedScale = 1;
		float bonusScale = Mathf.Lerp(0.2f,-0.125f,speedScale);
		scale += bonusScale; //Add how much less or more the player should be squished by to the scale
		switch(quadrant){
			case 1:
			case 3:
				if(convertedAngle > Q1_MIDPOINT) //X squished
					SpritesNode.Scale = new Vector2(Mathf.Lerp(SpritesNode.Scale.X,scale,LERP_AMOUNT),1);
				else //Y squished
					SpritesNode.Scale = new Vector2(1,Mathf.Lerp(SpritesNode.Scale.Y,scale,LERP_AMOUNT));
				break;
			case 2:
			case 4:
			case 5: //If angle is exactly 2π it will return in quadrant 5
				if(convertedAngle > Q1_MIDPOINT) //Y squished
					SpritesNode.Scale = new Vector2(1,Mathf.Lerp(SpritesNode.Scale.Y,scale,LERP_AMOUNT));
				else //X squished
					SpritesNode.Scale = new Vector2(Mathf.Lerp(SpritesNode.Scale.X,scale,LERP_AMOUNT),1);
				break;
		}
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
			if(!isRegaining) BounceEffects();
			isRegaining = true;
			if(Game.CurrentMode != Mode.GameMode.Golf){
				CanLaunch = true;
				CanSlam = true;
			} 
			stillTimer = 0;
			airTimer = 0;
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
		BounceEffects(LinesSprite.GlobalPosition,Rb.LinearVelocity,0);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.BounceParticle)]
	public void BounceEffects(Vector2 position,Vector2 velocity,byte preProcessTicks){
		if(BounceTimer >= BOUNCE_SFX_TIMEOUT){
			float velSquared = velocity.LengthSquared();
			if(velocity.Y > 100 || velocity.Y < -100){
				bounceSound.PitchScale = Game.Random.Next(80,110)/100f;
				bounceSound.Play(); 
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
		popParticles.GlobalPosition = LinesSprite.GlobalPosition;
		popParticles.Emitting = true;
		SFX.Play("Pop",popParticles.GlobalPosition);
	}

	public void _on_rigid_body_2d_body_exited(PhysicsBody2D body){	
		if(body.IsInGroup("Regain") || body.GetParent().IsInGroup("Regain")){
			isRegaining = false;
		}
	}

	//Controls
	private void FlameCharge(){
		if(LaunchPower > MAX_LAUNCH_POWER || (LaunchPower == MAX_LAUNCH_POWER && !flameParticles.Emitting)){
			LaunchPower = MAX_LAUNCH_POWER;
			flameParticles.Emitting = true;
			flameSound.Play();
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
				Rpc(nameof(Flip),InputVector.X < 0,InputVector.Angle() + (InputVector.X < 0 ? MathF.PI : 0));
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
					if(InputVector.X < 0) Flip(true,InputVector.Angle() + MathF.PI);
					else Flip(false,InputVector.Angle());
					TicksToIgnore = (int)Math.Ceiling((1-Online.Buffer)*(ping / (1000.0/Engine.PhysicsTicksPerSecond)));
				}else{
					GD.Print("Ping: " + ping);
					if(!Level.IsPositionOffscreen(Rb.GlobalPosition)){
						TicksToIgnore = PingGetter.PingToTicks(ping);
						if (!Mode.Finished) RpcId(1, nameof(SendLaunchToServerNRewind), InputVector.Angle(), LaunchPower, (byte)TicksToIgnore);
						if (InputVector.X < 0) Flip(true, InputVector.Angle() + MathF.PI);
						else Flip(false, InputVector.Angle());
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

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.PlayerText)]
	public void ShowPlayerText(){
		if(GetTree().GetMultiplayer().GetRemoteSenderId() == 0 || Online.IsRpcFromHost() || IsRpcFromPlayerOwner()){
			textTimer = 0;
			playerText.ZIndex = Rb.ZIndex + 1;
			string newPlayerText = Mode.ModeNode.GetPlayerText(this);
			if(!newPlayerText.Equals(playerText.Text)){
				playerText.Text = newPlayerText;
			}
		}
	}
	private void ShowUsernameText(){
		usernameText.SelfModulate = PlayerColor;
		usernameText.Visible = true;
	}
	private void HideUsernameText(){
		usernameGroup.Visible = false;
	}

	private static readonly float ONLINE_UPDATE_RATE = (float)PlayerSync.VISUAL_SYNC_INTERVAL / Engine.PhysicsTicksPerSecond;
	private static readonly float LOCAL_UPDATE_RATE = (float)1 / Engine.PhysicsTicksPerSecond;
	private void EyeAndArrowUpdate(float fDelta){
		//Update Arrow and eye positions & scale
		float lerpAmount;
		if(InputVector != Vector2.Zero && CanLaunch){
			if(fDelta <= ONLINE_UPDATE_RATE) lerpAmount = fDelta/(float)Engine.TimeScale / (OwnsPlayer() ? LOCAL_UPDATE_RATE : ONLINE_UPDATE_RATE);
			else lerpAmount = OwnsPlayer() ? 1f : ONLINE_UPDATE_RATE; //When FPS is lower than tickrate eye and arrow freak out this tries to fix it but fails :skull:
			Vector2 newArrowPosition = InputVector * 125;
			float newArrowRotation = InputVector.Angle();
			float arrowScale = (((LaunchPower + MIN_LAUNCH_POWER) / MAX_LAUNCH_POWER)*MAX_LAUNCH_TIME) + 1;
			Vector2 newArrowScale = new Vector2(arrowScale, arrowScale);
			//arrowSprite.Scale = arrowSprite.Scale.Lerp(new Vector2( (LaunchPower + MIN_LAUNCH_POWER)/ MAX_LAUNCH_POWER + 1 , (LaunchPower + MIN_LAUNCH_POWER)/MAX_LAUNCH_POWER + 1), arrowSprite.Scale.X < 1 ? 0.5f : lerpAmount);
			if(OwnsPlayer()){ //Owner
				arrowSprite.Scale = newArrowScale;
				bool bigAngDiff = MathF.Abs(arrowSprite.Rotation - newArrowRotation) > (float)(Math.PI/2.0) || arrowSprite.Scale == Vector2.Zero;
				if(bigAngDiff){
					//arrowSprite.Rotation = newArrowRotation;
					arrowSprite.Position = newArrowPosition;
				}else{
					arrowSprite.Position = arrowSprite.Position.Lerp(newArrowPosition,lerpAmount);
					//arrowSprite.Rotation = bigAngDiff ? newArrowRotation : Mathf.LerpAngle(arrowSprite.Rotation,newArrowRotation,lerpAmount);
				}
			}else{ //Non owner if there isn't a big angle difference it lerps
				if(arrowScale >= arrowSprite.Scale.X){
					arrowSprite.Scale = arrowSprite.Scale.Lerp(newArrowScale,lerpAmount);
				}else{
					arrowSprite.Scale = newArrowScale;
				}
				
				bool bigAngDiff = MathF.Abs(arrowSprite.Rotation - newArrowRotation) > (float)(5.0*Math.PI/6.0) || arrowSprite.Scale == Vector2.Zero;
				if(bigAngDiff){
					arrowSprite.Position = newArrowPosition;
				}else{
					float lerpedAngle = Mathf.LerpAngle(arrowSprite.Position.Angle(),newArrowPosition.Angle(),lerpAmount);
					arrowSprite.Position = Vector2.FromAngle(lerpedAngle)*newArrowPosition.Length();
				}
			}
			arrowSprite.Rotation = arrowSprite.Position.Angle();
		}else{
			arrowSprite.Scale = Vector2.Zero;
			lerpAmount = 1;
		}
		//Eyes
		if(PlayerEmotion != Emotion.Bumped){
			Vector2 eyePosition = RawInputVector * (6/TextureScale);
			eyePosition = eyePosition.Rotated(-(Rb.Rotation+RotationsNode.Rotation));
			PupilsSprite.Position = PupilsSprite.Position.Lerp(eyePosition,lerpAmount);
		}else PupilsSprite.Position = Vector2.Zero;
		
		if(LaunchPower == MAX_LAUNCH_POWER){ //Move this elsewhere
			PlayerEmotion = Emotion.Angry;
			emotionTimer = 1/3f;
		}
	}

	public void ItemButtonPressed(){
		if(Item != null && !ItemRouletteAnimation.Visible){
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

	private void Timers(float delta){
		if(textTimer >= 3) playerText.Text = "";
		else textTimer += delta;
	}

	private void Updates(float delta){

		if(textTimer >= 3) playerText.Text = "";
		else textTimer += delta;

		BounceTimer += delta;

		//Timers n Stuff
		//Item Roulette
		if(ItemRouletteAnimation.Visible){
			itemRouletteTimer += delta;
			if(itemRouletteTimer >= 2){
				itemSound.Play();
				ItemRouletteAnimation.Pause();
				ItemRouletteAnimation.Visible = false;
				itemSprite.Visible = true;
				itemTriangle.Visible = true;
				if(Item is TransformItem) TransformBar.Visible = true;
				itemRouletteTimer = 0;
			}
		}
		
		//Regain Checks
		if(isRegaining){
			if(Game.CurrentMode != Mode.GameMode.Golf){
				CanLaunch = true;
				CanSlam = true;
			}else if(Game.CurrentMode == Mode.GameMode.Golf){
				if(stillTimer >= 2){
					CanLaunch = true;
					CanSlam = true;
					setNewPos = false;
					stillTimer = 0;
					//Make this better
				}else if(stillTimer >= 1 && stillTimer < 2 && Mathf.Abs(Rb.GlobalPosition.DistanceTo(SpawnPoint)) < 512 && Golf.PlayerStrokes[Id-1] != 0) 
					PlayerEmotion = Emotion.Annoyed;
				stillTimer += delta;
			}
		}
		
		//Set Golf respawn points && deal with initial invulnerability
		if(Game.CurrentMode == Mode.GameMode.Golf){
			if(Invulnerable && Golf.PlayerStrokes[Id-1] == 0 && Game.TotalPlayers != 1) invulnerabilityTimer = 0.5f;
			if(!isRegaining && !setNewPos && !Level.IsPositionOffscreenOrDead(Rb.GlobalPosition)){
				setNewPos = true;
				SpawnPoint = Rb.GlobalPosition;
			}
		}
		
		//Gives player launch and slam back if stuck in air
		if(!CanLaunch || !CanSlam){
			if(airTimer <= 10) airTimer += delta;
			else{
				CanSlam = true;
				CanLaunch = true;
				airTimer = 0;
			}
		}

		//Transformation Timer and special abilities
		if(Item is TransformItem){
			TransformItem tItem = (TransformItem)Item;
			tItem.TransformItemTimer(delta);
			if(tItem.Activated){
				if(tItem is Wings) CanLaunch = true;
				else if(tItem is Moon && airTimer >= (Game.CurrentMode != Mode.GameMode.Golf ? 0.75f : 2)){
					CanLaunch = true;
					airTimer = 0;
				}
			}
		}

		//Invulnerability Timer
		if(Invulnerable){
			if(invulnerabilityTimer > 0) invulnerabilityTimer -= delta;
			else if(invulnerabilityTimer <= 0) Invulnerable = false;
		}

		//Emotion Timer
		if(emotionTimer > 0) emotionTimer -= delta;
		else if(PlayerEmotion != Emotion.Neutral) PlayerEmotion = Emotion.Neutral;

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
		Flip(FlippedStart);
		Rb.SetDeferred("global_position", SpawnPoint);
		Rb.SetDeferred("linear_velocity", Vector2.Zero);
		Rb.SkipInterpolation();
		Rb.SetDeferred("global_position", SpawnPoint);
		Rb.SetDeferred("linear_velocity", Vector2.Zero);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.PlayerFlip)]
	public void Flip(bool flipped){
		if(Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId() == 0 || IsRpcFromPlayerOwner() || Online.IsRpcFromHost()){
			LinesSprite.FlipH = flipped;
			EyesSprite.FlipH = flipped;
			PupilsSprite.FlipH = flipped;
			if(Game.CurrentMode == Mode.GameMode.CrownTheKing){
				Sprite2D crownSprite = SpritesNode.GetNodeOrNull<Sprite2D>("Crown");
				if(crownSprite != null){
					crownSprite.FlipH = flipped;
					int sign = flipped ? 1 : -1;
					crownSprite.Position = new Vector2(10*sign,-100)* playerScale;
					crownSprite.Rotation = CTK.CROWN_ANGLE * sign;
				}
			}
		}
	}
	public void FlipV(bool flipped){
		LinesSprite.FlipV = flipped;
		EyesSprite.FlipV = flipped;
		PupilsSprite.FlipV = flipped;
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.PlayerFlip)]
	public void Flip(bool flipped,float rotation){
		int senderId = Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId();
		if(senderId == 0 || IsRpcFromPlayerOwner() || Online.IsRpcFromHost()){
			if(senderId == 0 || !OwnsPlayer() || (Online.IsHost() && !isRegaining)) RotationsNode.GlobalRotation = rotation; //Rb.SetDeferred("global_rotation", rotation);
			CallDeferred(nameof(Flip), flipped);
		}
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
		Vector2 ogScale = SpritesNode.Scale;
		SpritesNode.Scale = Vector2.Zero;
		Tween scaleTween = CreateTween();
		scaleTween.TweenProperty(SpritesNode,"scale",ogScale,0.125f);
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
		FlipV(false);
		PlayerScale = 1;

		BallSprite.SelfModulate = new Color(BallSprite.SelfModulate,1);
		ShadingSprite.SelfModulate = new Color(ShadingSprite.SelfModulate, 1);
		OutlineSprite.SelfModulate = new Color(OutlineSprite.SelfModulate,1);
		foreach(Player player in Game.Players) if(player != null) Rb.RemoveCollisionExceptionWith(player.Rb);
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void StartItemRoulette(){
		if(Online.IsRpcFromHost()){
			ItemRouletteAnimation.Play();
			ItemRouletteAnimation.Visible = true;
			itemSprite.Visible = false;
			itemTriangle.Visible = true;
			TransformBar.Visible = false;
			RouletteSound.Play();
		}
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
				invulnerabilityTimer = 2;
				BallSprite.SelfModulate = new Color(BallSprite.SelfModulate,0.5f);
				ShadingSprite.SelfModulate = new Color(ShadingSprite.SelfModulate,0);
			}else{
				invulnerabilityTimer = 0;
				foreach(Player player in Game.Players) Rb.RemoveCollisionExceptionWith(player.Rb);
				BallSprite.SelfModulate = new Color(BallSprite.SelfModulate,1);	
				ShadingSprite.SelfModulate = new Color(ShadingSprite.SelfModulate,1);
			}
		}
	}

	public Item Item{
		get{return item;}
		set{
			item = value;
			if(item != null){
				itemSprite.Texture = item.Icon;
				itemTriangle.Visible = true;
				if(item is SingleUseItem) ItemAmountText.Text = (item as SingleUseItem).Amount.ToString();
			} 
			else{
				itemSprite.Texture = null;
				itemTriangle.Visible = false;
			} 
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
			HUDNode.Scale = new Vector2(playerScale,playerScale);
			CircleShape2D defaultCircle = new CircleShape2D();
			defaultCircle.Radius = RADIUS * playerScale;
			RbShape.Shape = defaultCircle;
			bool isBig = playerScale >= 1.99f;
			Vector2 newTextureScaleVector;
			if(isBig){
				float newTextureScale = (float)(DEFAULT_TEXTURE_SIZE * 2f) / (float)bigBallSize;
				newTextureScaleVector = new Vector2(newTextureScale,newTextureScale) * (playerScale/2f);
			}else{
				newTextureScaleVector = new Vector2(TextureScale,TextureScale) * playerScale;
			}
			LinesSprite.Scale = newTextureScaleVector;
			OutlineSprite.Scale = newTextureScaleVector;
			BallSprite.Texture = isBig ? BigBasketBall : BasketBallTexture;
			float ballSpriteScale;
			bool sameTexture = BigBasketBall.GetHeight() == BasketBallTexture.GetHeight();
			ballSpriteScale = (BallSize / (float)((isBig ? BigBasketBall : BasketBallTexture).GetHeight() * 2)) * TextureScale * playerScale;
			BallSprite.RegionRect = new Rect2(0,0,BallSprite.Texture.GetWidth()*2,BallSprite.Texture.GetHeight()*2);
			BallSprite.Scale = new Vector2(ballSpriteScale,ballSpriteScale);
			LinesSprite.Texture = isBig ? bigLinesTexture : LinesTexture;
			OutlineSprite.Texture = isBig ? BigOutlineTexture : OutlineTexture;
			ShadingSprite.Scale = new Vector2(0.38f * playerScale, 0.38f * playerScale);
			PlayerEmotion = PlayerEmotion;
			if(Game.CurrentMode == Mode.GameMode.CrownTheKing){
				Sprite2D crownSprite = SpritesNode.GetNodeOrNull<Sprite2D>("Crown");
				if(crownSprite != null){
					float crownScale = 114f/crownSprite.Texture.GetHeight();
					crownSprite.Scale = new Vector2(crownScale,crownScale) * playerScale;
					crownSprite.Position = new Vector2(10*(crownSprite.FlipH ? 1 : -1),-100)*playerScale;
				}
			}
		}
	}

	public enum Emotion{
		Happy, Sad, Angry, Annoyed, Shocked, Bumped, Neutral
	}

	public Emotion PlayerEmotion{
		get{return playerEmotion;}
		set{
			bool isBig = PlayerScale >= 1.99f;
			float emotionTime = Game.Random.NextSingle() + 2;
			playerEmotion = value;
			switch(playerEmotion){
				case Emotion.Happy:
					EyesSprite.Texture = isBig ? bigHappyEye : happyEye;
					PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
					emotionTimer = emotionTime;
					break;
				case Emotion.Sad:
					EyesSprite.Texture = isBig ? bigSadEye : sadEye;
					PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
					emotionTimer = emotionTime;
					break;
				case Emotion.Angry:
					EyesSprite.Texture = isBig ? bigAngryEye : angryEye;
					PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
					emotionTimer = emotionTime;
					break;
				case Emotion.Annoyed:
					EyesSprite.Texture = isBig ? bigAnnoyedEye : annoyedEye;
					PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
					emotionTimer = emotionTime;
					break;
				case Emotion.Shocked:
					EyesSprite.Texture = isBig ? bigShockedEye : shockedEye;
					PupilsSprite.Texture = isBig ? bigShockedPupil : shockedPupil;
					emotionTimer = emotionTime;
					break;
				case Emotion.Bumped:
					EyesSprite.Texture = isBig ? bigBumpedEye : bumpedEye;
					PupilsSprite.Texture = null;
					emotionTimer = 0.5f;
					break;
				default:
					EyesSprite.Texture = isBig ? bigNeutralEye : neutralEye;
					PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
					emotionTimer = 0;
					break;
			}
		}
	}

	public static Texture2D GetEyeTexture(Emotion emotion, bool isBig){
		switch(emotion){
			case Emotion.Happy: return isBig ? bigHappyEye : happyEye;
			case Emotion.Sad: return isBig ? bigSadEye : sadEye;
			case Emotion.Angry: return isBig ? bigAngryEye : angryEye;
			case Emotion.Annoyed: return isBig ? bigAnnoyedEye : annoyedEye;
			case Emotion.Shocked: return isBig ? bigShockedEye : shockedEye;
			case Emotion.Bumped: return isBig ? bigBumpedEye : bumpedEye;
			default: return isBig ? bigNeutralEye : neutralEye;
		}
	}

	public static Texture2D GetPupilTexture(Emotion emotion, bool isBig){
		switch(emotion){
			case Emotion.Shocked: return isBig ? bigShockedPupil : shockedPupil;
			case Emotion.Bumped: return null;
			default: return isBig ? bigNeutralPupil : neutralPupil;
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