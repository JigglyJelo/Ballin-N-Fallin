using Godot;

public partial class TheRing : Node2D{
	private const float SCALE_SPEED = 0.015f;
	private const float SPEED_PLAYER = 0.005f+0.000625f;
	private const float MIN_SCALE = 0.1f;
	private readonly Vector2 MIN_SCALE_VECTOR = new Vector2(MIN_SCALE,MIN_SCALE);
	public override void _PhysicsProcess(double delta){
		float scaleDelta = (float)delta * (SCALE_SPEED+(SPEED_PLAYER*(1f/Game.TotalPlayers)));
		if(Scale.X > 0.1f){
			Scale -= new Vector2(scaleDelta,scaleDelta);
		}else if(Scale.X < 0.1f){
			Scale = MIN_SCALE_VECTOR;
		}
	}

	public void _on_area_2d_body_exited(PhysicsBody2D body){
		if(Game.CurrentScene == Game.SceneType.Game){
			if(body.IsInGroup("Player")){
				Player player = body.GetParent() as Player;
				if(Online.IsHost()){
            	    Death.KillPlayer(player,Death.DeathCause.Pop);
            	}
			}
		}
	}
}