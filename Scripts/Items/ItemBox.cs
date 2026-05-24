using Godot;

public partial class ItemBox : Node{
	private static Texture2D[] itemBoxTextures;
	private float animationTimer = 0f;
	private const float ANIMATION_FRAME_TIME = 1f/12f;
	private int animationFrame = 0;
	private Polygon2D itemBoxPolygon;
	public ItemSpawner Creator;
	public InterpolatedBody Rb;
	private Node2D Smoother;
	public override void _Ready(){
        const int UNIQUE_ANIMATION_FRAMES = 11;
        if(Rb == null) Rb = GetNode<InterpolatedBody>("RigidBody2D"); 
        
        itemBoxPolygon = GetNode<Polygon2D>("Smoothing2D/Visuals/AntialiasedPolygon2D");
        
        if(itemBoxTextures == null){
            const int ANIMATION_FRAMES_LENGTH = 20;
            itemBoxTextures = new Texture2D[ANIMATION_FRAMES_LENGTH];
            for(int i = 0; i < UNIQUE_ANIMATION_FRAMES; i++) itemBoxTextures[i] = GD.Load<Texture2D>("res://Assets/Sprites/Items/Item Box/ItemBox (" + (i + 1) + ").png");
            for(int i = UNIQUE_ANIMATION_FRAMES; i < ANIMATION_FRAMES_LENGTH; i++) itemBoxTextures[i] =  itemBoxTextures[ANIMATION_FRAMES_LENGTH - i];
        }
        
        try{
            foreach (Player player in Game.Players){
                Rb.AddCollisionExceptionWith(player.Rb);
            }
        }catch{}

        animationFrame = Game.Random.Next(0,UNIQUE_ANIMATION_FRAMES);
        itemBoxPolygon.Texture = itemBoxTextures[animationFrame];

        // Ensure Visuals scale up from zero perfectly from the spawner
        Node2D visuals = GetNode<Node2D>("Smoothing2D");
        visuals.Scale = Vector2.Zero;
        Tween scaleTween = CreateTween();
        scaleTween.TweenProperty(visuals, "scale", Vector2.One, 0.2);
    }

	public void BodyEnteredSignal(PhysicsBody2D body){
		if(body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			if(Online.IsHost()){
				if(player.Item == null){
					player.Rpc(nameof(player.StartItemRoulette));
					//Calculates Item on host and sends result
					Creator.SendItemToPlayer(player.Id);
				}
				Creator.Rpc(nameof(Creator.RemoveItemBox),true);
			}
		}
	}

    public override void _PhysicsProcess(double delta){
        animationTimer += (float)delta;
		if(animationTimer > ANIMATION_FRAME_TIME){
			animationTimer = 0f;
			if(++animationFrame == itemBoxTextures.Length) animationFrame = 0;
			itemBoxPolygon.Texture = itemBoxTextures[animationFrame];
		}
    }
}