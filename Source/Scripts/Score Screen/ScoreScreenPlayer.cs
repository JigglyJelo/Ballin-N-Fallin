using Godot;

public partial class ScoreScreenPlayer : RigidBody2D{
	public int Id;
	public Sprite2D EyeSprite;
	private Sprite2D ballSprite,shadingSprite,lineSprite,outlineSprite,pupilSprite;
	private Vector2 inputVector;
	private int inputId;
	private bool canSlam = true;
	private AudioStreamPlayer2D bounceSound;
	public override void _Ready(){
		
		Id = (GetParent() as EndscreenResult).Id;
		inputId = (int)Game.PlayerDatas[Id-1].InputDevice;
		Color color = Game.GetPlayerColor(Id);
		ballSprite = GetNode<Sprite2D>("BallSprite");
		shadingSprite = GetNode<Sprite2D>("ShadingSprite");
		lineSprite = GetNode<Sprite2D>("LinesSprite");
		EyeSprite = GetNode<Sprite2D>("EyeSprite");
		pupilSprite = GetNode<Sprite2D>("EyeSprite/PupilSprite");
		outlineSprite = GetNode<Sprite2D>("OutlineSprite");
		bounceSound = GetNode<AudioStreamPlayer2D>("BounceSound");
		if(Id > Game.TotalPlayers / 2){
			lineSprite.FlipH = true;
			EyeSprite.FlipH = true;
			pupilSprite.FlipH = true;
		}
		Vector2 textureScale = new Vector2(Player.TextureScale,Player.TextureScale);
        ballSprite.Texture = Player.BasketBallTexture;
		float ballSpriteScale = (Player.BallSize/(float)(Player.BasketBallTexture.GetHeight()*2) * Player.TextureScale);
		ballSprite.Scale = new Vector2(ballSpriteScale,ballSpriteScale);
		ballSprite.RegionRect = new Rect2(0,0,ballSprite.Texture.GetWidth()*2,ballSprite.Texture.GetHeight()*2);
		ballSprite.SelfModulate = color;
		shadingSprite.SelfModulate = color;
		lineSprite.Texture = Player.LinesTexture;
		lineSprite.SelfModulate = Colors.Black;
		lineSprite.Scale = textureScale;
		EyeSprite.Texture = Player.GetEyeTexture(Player.Emotion.Neutral,false);
		EyeSprite.Scale = textureScale;
		pupilSprite.Texture = Player.GetPupilTexture(Player.Emotion.Neutral,false);
		outlineSprite.Texture = Player.OutlineTexture;
		outlineSprite.SelfModulate = Colors.Black;
		outlineSprite.Scale = textureScale;
		if(Tour.IsTour){
			int playerScore = Tour.PlayerScores[Id-1];
			if(playerScore == 0){
				EyeSprite.Texture = Player.GetEyeTexture(Player.Emotion.Shocked,false);
				pupilSprite.Texture = Player.GetPupilTexture(Player.Emotion.Shocked,false);
			}
		}
	}

	public override void _PhysicsProcess(double delta){
		if(inputId != (int)PlayerData.PlayerInputDevice.Mouse && inputId != (int)PlayerData.PlayerInputDevice.None){
            inputVector = Input.GetVector("Aim Left" + inputId,"Aim Right" + inputId,"Aim Up" + inputId,"Aim Down" + inputId);
            if(inputVector.IsZeroApprox()) inputVector = Input.GetVector("DPad Left" + inputId,"DPad Right" + inputId,"DPad Up" + inputId,"DPad Down" + inputId);
			if(Input.IsActionJustPressed("Slam" + inputId) && canSlam){
				slam();
			}
        }else if(inputId == (int)PlayerData.PlayerInputDevice.Mouse){
            inputVector = Input.GetVector("Left Keyboard", "Right Keyboard", "Up Keyboard", "Down Keyboard");
			if(Input.IsActionJustPressed("Slam Mouse") && canSlam){
				slam();
			}
        }
		

		if(true) pupilSprite.Position = inputVector * (6/Player.TextureScale); //OwnsPlayer
		void slam(){
			LinearVelocity = new Vector2(LinearVelocity.X, LinearVelocity.Y * 0.5f);
			LinearVelocity += Vector2.Down * Player.SLAM_POWER * (GravityScale == 0 ? 1 : GravityScale/Player.GRAVITY);
			canSlam = false;
		}
	}

	public void Collision(Node collision){
		canSlam = true;
		if(!bounceSound.Playing){
			bounceSound.PitchScale = Game.Random.Next(80,110)/100f;
			bounceSound.Play();
		}
	}
}