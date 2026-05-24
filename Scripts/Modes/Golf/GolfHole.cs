using Godot;

public partial class GolfHole : Area2D{
	private Polygon2D flag;

	public async override void _Ready(){
		float width = Level.OUTLINE_WIDTH / ((GetParent() as Level).CameraZoom * (1 + (1f/3f)));
		Line2D line = GetNode<Line2D>("TopOutline");
		flag = GetNode<Polygon2D>("Pole/Flag");
		await ToSignal(GetTree().CreateTimer(0.01f, false), "timeout");
		line.DefaultColor = Level.LevelNode.OutlineColorOverride;
		line = GetNode<Line2D>("BottomOutline");
		line.DefaultColor = Level.LevelNode.OutlineColorOverride;
		Mode.AddCameraTarget(this);
		//Mode.AddCameraTarget(flag);
	}

    public override void _Process(double delta){
		flag.TextureOffset = new Vector2(flag.TextureOffset.X + (float)delta * 200, 0);
    }
	
	public void _on_area_2d_body_entered(PhysicsBody2D body) {
        if(body.GetParent().IsInGroup("Player")){
			Player player = body.GetParent() as Player;
            if(Online.IsHost()){
				Golf golfNode = Mode.ModeNode as Golf;
				golfNode.Rpc(nameof(golfNode.PlayerInHole),player.Id);
			}
		}
	}
}