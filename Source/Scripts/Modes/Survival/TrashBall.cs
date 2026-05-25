using Godot;
using System.Linq;

public partial class TrashBall : Trash{
    private Sprite2D shadingSprite;
    public override void _Ready(){
        base._Ready();
        Sprite2D ballSprite = GetNode<Sprite2D>("Smoothing2D/BallSprite");
        shadingSprite = GetNode<Sprite2D>("Smoothing2D/Shading");
        if(Game.CurrentMode == Mode.GameMode.Survival && Game.CurrentLevelName.Contains("Trash Compactor - ")){
            TrashCompactor trashCompactor = Mode.ModeNode.GetNode<TrashCompactor>("Level/TrashSpawner");
            ballSprite.SelfModulate = Game.Colors.ElementAt(trashCompactor.BallColorRandom.Next(0,Game.Colors.Count)).Value;
            shadingSprite.SelfModulate = ballSprite.SelfModulate;
        }
		if(Player.BallSize != 190){
        	ballSprite.Texture = Player.BasketBallTexture;
			float ballSpriteScale = (Player.BallSize/(float)(Player.BasketBallTexture.GetHeight()*2) * Player.TextureScale);
			ballSprite.Scale = new Vector2(ballSpriteScale,ballSpriteScale);
			ballSprite.RegionRect = new Rect2(0,0,ballSprite.Texture.GetWidth()*2,ballSprite.Texture.GetHeight()*2);
			Sprite2D lineSprite = GetNode<Sprite2D>("Smoothing2D/LineSprite");
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

    public void _on_rigid_body_2d_body_entered(PhysicsBody2D body){
        if(Online.IsHost() && body.IsInGroup("Player")){
            Player player = body.GetParent() as Player;
            if(player.IsStomping){
                player.Rpc(nameof(player.PlayerStomped));
                player.Rb.LinearVelocity = new Vector2(player.Rb.LinearVelocity.X,-2000f);
                ParticleManager.SpawnPopParticles(Rb.GlobalPosition, GetNode<Sprite2D>("Smoothing2D/BallSprite").SelfModulate);
                if(Game.CurrentMode == Mode.GameMode.Survival && Game.CurrentLevelName.Contains("Trash Compactor - ")){
                    TrashCompactor trashCompactor = Mode.ModeNode.GetNode<TrashCompactor>("Level/TrashSpawner");
                    byte key = trashCompactor.SpawnedTrash.FirstOrDefault(x => x.Value.Rb == Rb).Key;
                    trashCompactor.Rpc(nameof(trashCompactor.RemoveTrash),key);
                }
            }
        }
    }
}