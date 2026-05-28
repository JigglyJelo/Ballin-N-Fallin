using Godot;
using System;

public partial class PlayerVisuals : Node2D{
	private static readonly float ONLINE_UPDATE_RATE = (float)PlayerSync.VISUAL_SYNC_INTERVAL / Engine.PhysicsTicksPerSecond;
	private static readonly float LOCAL_UPDATE_RATE = (float)1 / Engine.PhysicsTicksPerSecond;
	public const int DEFAULT_TEXTURE_SIZE = 190; //The size of the default resolution (2160p) player sprites
	private const float MIN_STRETCH_SPEED = 2300; //The minimum velocity before the squash n stretch effect starts
	private Player player;

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
	private float textTimer = 3, itemRouletteTimer, emotionTimer;
	public Label ItemAmountText;
	private Label playerText, usernameText;
	private CanvasGroup usernameGroup;
	public TextureProgressBar TransformBar;
	public Node2D SpritesNode, RotationsNode, HUDNode;
	private Polygon2D itemTriangle;
	public void VisualsReady(Player player){
		if(this.player != null){
			GD.PrintErr("YOU CALLED Visuals.VisualsReady OUTSIDE OF THE PLAYER'S _READY");
			return;
		}
		this.player = player;
		SpritesNode = GetNode<Node2D>("Sprites");
		RotationsNode = SpritesNode.GetNode<Node2D>("Rotation");
		HUDNode = GetNode<Node2D>("HUD");
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
		
		BallSprite.SelfModulate = player.PlayerColor;
		arrowSprite.SelfModulate = player.PlayerColor;
		playerText.SelfModulate = player.PlayerColor;
		TransformBar.SelfModulate = player.PlayerColor;
		itemTriangle.Color = player.PlayerColor;
		ShadingSprite.SelfModulate = player.PlayerColor;
		arrowSprite.ZIndex = 1;
		if(Game.TotalPlayers != 2){
			switch(player.Team){
				case "A": OutlineSprite.SelfModulate = Game.TeamColors[0]; arrowSprite.SelfModulate = Game.TeamColors[0]; break;
				case "B": OutlineSprite.SelfModulate = Game.TeamColors[1]; arrowSprite.SelfModulate = Game.TeamColors[1]; break;
				case "C": OutlineSprite.SelfModulate = Game.TeamColors[2]; arrowSprite.SelfModulate = Game.TeamColors[2]; break;
				case "D":  OutlineSprite.SelfModulate = Game.TeamColors[3]; arrowSprite.SelfModulate = Game.TeamColors[3]; break;
			}
		}

		usernameText.Text = Game.GetUsername(player.Id);
		ShowUsernameText();
		if(!AccessibilityMenu.AlwaysShowNames){
			Tween usernameTween = CreateTween();
			usernameTween.TweenProperty(usernameGroup,"self_modulate",new Color(player.PlayerColor,0),3);
			usernameTween.TweenCallback(Callable.From(HideUsernameText));
		}
	}

    public override void _Process(double delta){
		float fDelta = (float)delta;
		EyeAndArrowUpdate(fDelta);
		Updates(fDelta);
        ShadingSprite.GlobalRotation = 0;
    }

	private void Updates(float delta){
		if(textTimer >= 3) playerText.Text = "";
		else textTimer += delta;

		//Item Roulette
		if(ItemRouletteAnimation.Visible){
			itemRouletteTimer += delta;
			if(itemRouletteTimer >= 2){
				player.ItemSound.Play();
				ItemRouletteAnimation.Pause();
				SetRouletteSpriteVisibility(false);
				SetItemSpriteVisibility(true);
				itemTriangle.Visible = true;
				if(player.Item is TransformItem) TransformBar.Visible = true;
				itemRouletteTimer = 0;
			}
		}

		//Emotion Timer
		if(emotionTimer > 0) emotionTimer -= delta;
		else if(player.PlayerEmotion != Player.Emotion.Neutral) player.PlayerEmotion = Player.Emotion.Neutral;
	}

	private void EyeAndArrowUpdate(float fDelta){
		//Update Arrow and eye positions & scale
		float lerpAmount;
		if(player.InputVector != Vector2.Zero && player.CanLaunch){
			if(fDelta <= ONLINE_UPDATE_RATE) lerpAmount = fDelta/(float)Engine.TimeScale / (player.OwnsPlayer() ? LOCAL_UPDATE_RATE : ONLINE_UPDATE_RATE);
			else lerpAmount = player.OwnsPlayer() ? 1f : ONLINE_UPDATE_RATE; //When FPS is lower than tickrate eye and arrow freak out this tries to fix it but fails :skull:
			Vector2 newArrowPosition = player.InputVector * 125;
			float newArrowRotation = player.InputVector.Angle();
			float arrowScale = (((player.LaunchPower + PlayerPhysics.MIN_LAUNCH_POWER) / PlayerPhysics.MAX_LAUNCH_POWER)*PlayerPhysics.MAX_LAUNCH_TIME) + 1;
			Vector2 newArrowScale = new Vector2(arrowScale, arrowScale);
			//arrowSprite.Scale = arrowSprite.Scale.Lerp(new Vector2( (LaunchPower + MIN_LAUNCH_POWER)/ MAX_LAUNCH_POWER + 1 , (LaunchPower + MIN_LAUNCH_POWER)/MAX_LAUNCH_POWER + 1), arrowSprite.Scale.X < 1 ? 0.5f : lerpAmount);
			if(player.OwnsPlayer()){ //Owner
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
		if(player.PlayerEmotion != Player.Emotion.Bumped){
			Vector2 eyePosition = player.RawInputVector * (6/TextureScale);
			eyePosition = eyePosition.Rotated(-(player.Rb.Rotation+RotationsNode.Rotation));
			PupilsSprite.Position = PupilsSprite.Position.Lerp(eyePosition,lerpAmount);
		}else PupilsSprite.Position = Vector2.Zero;
		
		if(player.LaunchPower == PlayerPhysics.MAX_LAUNCH_POWER){ //Move this elsewhere
			player.PlayerEmotion = Player.Emotion.Angry;
			emotionTimer = 1/3f;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.PlayerFlip)]
	public void Flip(bool flipped){
		if(Game.GameNode.GetTree().GetMultiplayer().GetRemoteSenderId() == 0 || player.IsRpcFromPlayerOwner() || Online.IsRpcFromHost()){
			LinesSprite.FlipH = flipped;
			EyesSprite.FlipH = flipped;
			PupilsSprite.FlipH = flipped;
			if(Game.CurrentMode == Mode.GameMode.CrownTheKing){
				Sprite2D crownSprite = SpritesNode.GetNodeOrNull<Sprite2D>("Crown");
				if(crownSprite != null){
					crownSprite.FlipH = flipped;
					int sign = flipped ? 1 : -1;
					crownSprite.Position = new Vector2(10*sign,-100)* player.PlayerScale;
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
		if(senderId == 0 || player.IsRpcFromPlayerOwner() || Online.IsRpcFromHost()){
			if(senderId == 0 || !player.OwnsPlayer() || (Online.IsHost() && !player.IsRegaining)) RotationsNode.GlobalRotation = rotation; //Rb.SetDeferred("global_rotation", rotation);
			CallDeferred(nameof(Flip), flipped);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,TransferChannel = (int)Online.TransferChannelEnum.PlayerText)]
	public void ShowPlayerText(){
		if(GetTree().GetMultiplayer().GetRemoteSenderId() == 0 || Online.IsRpcFromHost() || player.IsRpcFromPlayerOwner()){
			textTimer = 0;
			playerText.ZIndex = player.Rb.ZIndex + 1;
			string newPlayerText = Mode.ModeNode.GetPlayerText(player);
			if(!newPlayerText.Equals(playerText.Text)){
				playerText.Text = newPlayerText;
			}
		}
	}

	public void SetPlayerEmotionalSprites(Player.Emotion emotion){
		bool isBig = player.PlayerScale >= 1.99f;
			float emotionTime = Game.Random.NextSingle() + 2;
		switch(emotion){
			case Player.Emotion.Happy:
				EyesSprite.Texture = isBig ? bigHappyEye : happyEye;
				PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
				emotionTimer = emotionTime;
				break;
			case Player.Emotion.Sad:
				EyesSprite.Texture = isBig ? bigSadEye : sadEye;
				PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
				emotionTimer = emotionTime;
				break;
			case Player.Emotion.Angry:
				EyesSprite.Texture = isBig ? bigAngryEye : angryEye;
				PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
				emotionTimer = emotionTime;
				break;
			case Player.Emotion.Annoyed:
				EyesSprite.Texture = isBig ? bigAnnoyedEye : annoyedEye;
				PupilsSprite.Texture = isBig ? bigNeutralPupil : neutralPupil;
				emotionTimer = emotionTime;
				break;
			case Player.Emotion.Shocked:
				EyesSprite.Texture = isBig ? bigShockedEye : shockedEye;
				PupilsSprite.Texture = isBig ? bigShockedPupil : shockedPupil;
				emotionTimer = emotionTime;
				break;
			case Player.Emotion.Bumped:
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

	public static Texture2D GetEyeTexture(Player.Emotion emotion, bool isBig){
		switch(emotion){
			case Player.Emotion.Happy: return isBig ? bigHappyEye : happyEye;
			case Player.Emotion.Sad: return isBig ? bigSadEye : sadEye;
			case Player.Emotion.Angry: return isBig ? bigAngryEye : angryEye;
			case Player.Emotion.Annoyed: return isBig ? bigAnnoyedEye : annoyedEye;
			case Player.Emotion.Shocked: return isBig ? bigShockedEye : shockedEye;
			case Player.Emotion.Bumped: return isBig ? bigBumpedEye : bumpedEye;
			default: return isBig ? bigNeutralEye : neutralEye;
		}
	}

	public static Texture2D GetPupilTexture(Player.Emotion emotion, bool isBig){
		switch(emotion){
			case Player.Emotion.Shocked: return isBig ? bigShockedPupil : shockedPupil;
			case Player.Emotion.Bumped: return null;
			default: return isBig ? bigNeutralPupil : neutralPupil;
		}
	}

	public void SetItemSpriteTexture(){
		if(player.Item != null){
			itemSprite.Texture = player.Item.Icon;
		}else{
			itemSprite.Texture = null;
			itemTriangle.Visible = false;
		}
	}

	public void SetItemSpriteVisibility(bool visible){
		itemSprite.Visible = visible;
		usernameGroup.Position = visible ? new Vector2(0, -96) : Vector2.Zero;
		if(!visible){
			itemSprite.Texture = null;
			itemTriangle.Visible = false;
		}else{
			itemSprite.Texture = player.Item.Icon;
			itemTriangle.Visible = true;
			if(player.Item is SingleUseItem) ItemAmountText.Text = (player.Item as SingleUseItem).Amount.ToString();
		}
	}

	public void SetRouletteSpriteVisibility(bool visible){
		ItemRouletteAnimation.Visible = visible;
		usernameGroup.Position = visible ? new Vector2(0, -96) : Vector2.Zero;
		if(visible){
			itemSprite.Visible = false;
			itemTriangle.Visible = true;
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void StartItemRoulette(){
		if(Online.IsRpcFromHost()){
			ItemRouletteAnimation.Play();
			SetItemSpriteVisibility(false);
			SetRouletteSpriteVisibility(true);
			itemTriangle.Visible = true;
			TransformBar.Visible = false;
			player.RouletteSound.Play();
		}
	}

	public void ResetSquashNStretch(){
		SpritesNode.Scale = SpritesNode.Scale.Lerp(Vector2.One,0.125f);
		SpritesNode.Skew = Mathf.Lerp(SpritesNode.Skew,0,0.125f);
	}

	public void SquashNStretch(float velocityMagnitude){
		const float LERP_AMOUNT = 0.125f;
		//Modify scale and skew based off velocity and rotation to give illusion of stretching in direction of velocity
		float angle = player.Rb.ToLocal(player.Rb.GlobalPosition + player.Rb.LinearVelocity).Angle();
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

	public void ResetPlayerScale(){
		bool isBig = player.PlayerScale >= 1.99f;
		Vector2 newTextureScaleVector;
		if(isBig){
			float newTextureScale = (float)(DEFAULT_TEXTURE_SIZE * 2f) / (float)bigBallSize;
			newTextureScaleVector = new Vector2(newTextureScale,newTextureScale) * (player.PlayerScale/2f);
		}else{
			newTextureScaleVector = new Vector2(TextureScale,TextureScale) * player.PlayerScale;
		}
		LinesSprite.Scale = newTextureScaleVector;
		OutlineSprite.Scale = newTextureScaleVector;
		BallSprite.Texture = isBig ? BigBasketBall : BasketBallTexture;
		float ballSpriteScale;
		bool sameTexture = BigBasketBall.GetHeight() == BasketBallTexture.GetHeight();
		ballSpriteScale = (BallSize / (float)((isBig ? BigBasketBall : BasketBallTexture).GetHeight() * 2)) * TextureScale * player.PlayerScale;
		BallSprite.RegionRect = new Rect2(0,0,BallSprite.Texture.GetWidth()*2,BallSprite.Texture.GetHeight()*2);
		BallSprite.Scale = new Vector2(ballSpriteScale,ballSpriteScale);
		LinesSprite.Texture = isBig ? bigLinesTexture : LinesTexture;
		OutlineSprite.Texture = isBig ? BigOutlineTexture : OutlineTexture;
		ShadingSprite.Scale = new Vector2(0.38f * player.PlayerScale, 0.38f * player.PlayerScale);
		SetPlayerEmotionalSprites(player.PlayerEmotion);
		if(Game.CurrentMode == Mode.GameMode.CrownTheKing){
			Sprite2D crownSprite = SpritesNode.GetNodeOrNull<Sprite2D>("Crown");
			if(crownSprite != null){
				float crownScale = 114f/crownSprite.Texture.GetHeight();
				crownSprite.Scale = new Vector2(crownScale,crownScale) * player.PlayerScale;
				crownSprite.Position = new Vector2(10*(crownSprite.FlipH ? 1 : -1),-100)*player.PlayerScale;
			}
		}
	}

	private void ShowUsernameText(){
		usernameText.SelfModulate = player.PlayerColor;
		usernameText.Visible = true;
	}
	private void HideUsernameText(){
		usernameGroup.Visible = false;
	}
}