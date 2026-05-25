using Godot;
using System;

public partial class BallScript : SpawnableItemScript{
    public Player Player;
    public InterpolatedBody Rb;
    private AudioStreamPlayer2D bounceSound;
    private float bounceTimer = 0;
    private Sprite2D shadingSprite;
    public override void _Ready(){
        base._Ready();
        if(Player == null) Player = Game.Players[new Random().Next(0,Game.Players.Length)];
        lifetime = 10;
        shadingSprite = GetNode<Sprite2D>("Smoothing2D/Shading");
        if(Rb == null) Rb = GetNode<InterpolatedBody>("RigidBody2D");

        bounceSound = GetNode<AudioStreamPlayer2D>("RigidBody2D/BounceSound");
        Color color = Player.PlayerColor;
        GetNode<Node2D>("Smoothing2D").Visible = true;
        GetNode<Sprite2D>("Smoothing2D/BallSprite").SelfModulate = color;
        shadingSprite.SelfModulate = color;
        
        if (Online.IsHost()) Rb.CallDeferred("apply_impulse", Player.InputVector * 2000);
        Rb.AddCollisionExceptionWith(Player.Rb);
        
        if(Player.BallSize != 190){
            Sprite2D ballSprite = GetNode<Sprite2D>("Smoothing2D/BallSprite");
            ballSprite.Texture = Player.BasketBallTexture;
            float ballSpriteScale = (Player.BallSize/(float)(Player.BasketBallTexture.GetHeight()*2)) * Player.TextureScale;
            ballSprite.Scale = new Vector2(ballSpriteScale,ballSpriteScale);
            ballSprite.RegionRect = new Rect2(0,0,ballSprite.Texture.GetWidth()*2,ballSprite.Texture.GetHeight()*2);
            Sprite2D lineSprite = GetNode<Sprite2D>("Smoothing2D/LinesSprite");
            Vector2 textureScale = new Vector2(Player.TextureScale,Player.TextureScale);
            lineSprite.Scale = textureScale;
            lineSprite.Texture = Player.LinesTexture;
            Sprite2D outlineSprite = GetNode<Sprite2D>("Smoothing2D/OutlineSprite");
            outlineSprite.Texture = Player.OutlineTexture;
            outlineSprite.Scale = textureScale;
        }
    }
    public override void _Process(double delta){
        shadingSprite.GlobalRotation = 0;
    }
    public override void _PhysicsProcess(double delta){
        base._PhysicsProcess(delta);
        bounceTimer += (float)delta;
        if(Rb != null && timer >= 0.2f && Player != null && !IsQueuedForDeletion()) Rb.RemoveCollisionExceptionWith(Player.Rb);
    }
    public void _on_rigid_body_2d_body_entered(PhysicsBody2D body){
    	if(body.GetParent().IsInGroup("Regain")){
			if(bounceTimer >= 0.2f && MathF.Abs(Rb.LinearVelocity.Y) > 100){
				bounceSound.PitchScale = new Random().Next(80,110)/100f;
				bounceSound.Play();
				bounceTimer = 0;
			}
    	}else if(body.IsInGroup("Player")){
            Player player = body.GetParent() as Player;
            if(player.IsStomping && isSuccessfulStomp()){
                if(Online.IsHost()){
                    GD.Print("STOMP");
                    DeleteItem();
                    player.Rpc(nameof(player.PlayerStomped));
                    player.Rb.LinearVelocity = new Vector2(player.Rb.LinearVelocity.X,-2000f);
                }
            }else{
                player.PlayerEmotion = Player.Emotion.Bumped;
			    if(player.Rb.Freeze && !player.Finished){
				    player.Rb.SetDeferred("freeze",false);
				    GD.Print("Unfrozen");
			    }
            }

            bool isSuccessfulStomp(){
                return player.Rb.GlobalPosition.Y <= Rb.GlobalPosition.Y && //Stomping player above stomped player
				MathF.Abs(player.Rb.GlobalPosition.X-Rb.GlobalPosition.X) <= 150f && //Stomping player is horizontally aligned with stomper
				player.PlayerScale >= 1;
            }
        }
    }
}