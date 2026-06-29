using Godot;

public partial class DroppedCoin : Node{
	public byte Id;
	public InterpolatedBody Rb;
	private bool growing = false;
	public Sprite2D Sprite;
	private readonly Vector2 MIN_SCALE = new Vector2(0.05f,1);
	public float LifeTimer = LIFETIME;
	private bool collectable = false;
	private const float LIFETIME = 4.5f;
	private const float UNCOLLECTABLE_TIME = 0.25f;

	public override void _Ready(){
		Rb = GetNode<InterpolatedBody>("RigidBody2D");
		Sprite = GetNode<Sprite2D>("Smoothing2D/Sprite2D");
		SetPhysicsProcess(Online.IsHost());
	}

	public override void _Process(double delta){
		Sprite.Scale = BTTB.CoinScale;
        Sprite.Texture = BTTB.COIN_TEXTURES[BTTB.AnimationFrame];
		if(BTTB.AnimationFrame == 3) Sprite.FlipH = true;
		else if(BTTB.AnimationFrame == 0) Sprite.FlipH = false;
	}

	//Only runs on host (Set in Ready)
    public override void _PhysicsProcess(double delta){
        LifeTimer -= (float)delta;
		if(collectable){
			if(LifeTimer <= 0) BTTB.RpcRemoveDroppedCoin(Id);
		}else{
			if(LifeTimer < LIFETIME - UNCOLLECTABLE_TIME){
				collectable = true;
			}
		}
    }


	public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(Online.IsHost()){
			if(collectable){
				if(body.IsInGroup("Player")){
					Player player = body.GetParent() as Player;
					BTTB.RpcDroppedCoinCollected(Id,player.Id);
				}
			}else{
				if(body is StaticBody2D){
					collectable = true;
				}
			}
		}
	}
}