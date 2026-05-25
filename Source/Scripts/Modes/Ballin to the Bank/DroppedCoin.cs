using Godot;

public partial class DroppedCoin : Node{
	public byte Id;
	public InterpolatedBody Rb;
	private bool growing = false;
	public Sprite2D Sprite;
	private readonly Vector2 MIN_SCALE = new Vector2(0.05f,1);
	public float LifeTimer = LIFETIME;
	private const float LIFETIME = 4.5f;
	private const float UNCOLLECTABLE_TIME = 0.25f;

	public override void _Ready(){
		Rb = GetNode<InterpolatedBody>("RigidBody2D");
		Sprite = GetNode<Sprite2D>("Smoothing2D/Sprite2D");
	}

	public override void _Process(double delta){
		Sprite.Scale = BTTB.CoinScale;
        Sprite.Texture = BTTB.COIN_TEXTURES[BTTB.AnimationFrame];
		if(BTTB.AnimationFrame == 3) Sprite.FlipH = true;
		else if(BTTB.AnimationFrame == 0) Sprite.FlipH = false;
		LifeTimer -= (float)delta;
		if(LifeTimer <= 0) BTTB.RemoveDroppedCoin(Id);
	}

	public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(Online.IsHost()){
			if(body.IsInGroup("Player") && LifeTimer < LIFETIME - UNCOLLECTABLE_TIME){
				Player player = body.GetParent() as Player;
				BTTB.RpcDroppedCoinCollected(Id,player.Id);
			}
		}
	}
}