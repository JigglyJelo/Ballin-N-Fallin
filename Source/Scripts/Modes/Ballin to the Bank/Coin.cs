using Godot;

public partial class Coin : Node2D{
	public byte Id;
	private bool growing = false;
	public Sprite2D Sprite;
	private readonly Vector2 MIN_SCALE = new Vector2(0.05f,1);
	

	public override void _Ready(){
		Sprite = GetNode<Sprite2D>("Sprite2D");
		
	}

    public override void _Process(double delta){
		Sprite.Scale = BTTB.CoinScale;
        Sprite.Texture = BTTB.COIN_TEXTURES[BTTB.AnimationFrame];
		if(BTTB.AnimationFrame == 3) Sprite.FlipH = true;
		else if(BTTB.AnimationFrame == 0) Sprite.FlipH = false;
    }


	public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(Online.IsHost()){
			if(body.IsInGroup("Player")){
				Player player = body.GetParent() as Player;
				BTTB.RpcCoinCollected(Id,player.Id);
			}
		}
	}
}