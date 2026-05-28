using Godot;

public partial class DominationZone : Polygon2D{
	private Player controllingPlayer;
	private Line2D outline;
	public Player ControllingPlayer{
		get{return controllingPlayer;}
		set{
			if(controllingPlayer != value){
				controllingPlayer = value;
				UpdateVisual();
			}
		}
	}
	public override void _Ready(){
		outline = GetNode<Line2D>("Line2D");
	}

	public override void _PhysicsProcess(double delta){
		if(!Domination.Finished && ControllingPlayer != null){
			int lastDisplayedScore = (int)Domination.Scores[ControllingPlayer.Id-1];
			Domination.Scores[ControllingPlayer.Id-1] += ((float)delta / (Domination.ZoneCount/2f)) *1.25f;
			if(MusicPlayer.GetPitch() != Domination.FAST_MUSIC_SPEED && Domination.Scores[ControllingPlayer.Id-1] > Domination.TotalScore * 0.75f){
				MusicPlayer.SetPitch(Domination.FAST_MUSIC_SPEED);
			}
			if(lastDisplayedScore != (int)Domination.Scores[ControllingPlayer.Id-1]){
				ControllingPlayer.Visuals.ShowPlayerText();
			}
			if(Online.IsHost() && Domination.Scores[ControllingPlayer.Id-1] >= Domination.TotalScore){
				Domination.GameFinished();
			}
		}
	}

    public override void _Process(double delta){
        if(controllingPlayer != null){
			float speed = 100 * (float)delta;
			TextureOffset -= new Vector2(speed, speed);
		}
    }


	public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(Online.IsHost() && body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			Rpc(nameof(SyncControllingPlayer),player.Id);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SyncControllingPlayer(byte id){
		if(id == 0) ControllingPlayer = null;
		else ControllingPlayer = Game.Players[id-1];
	}

	private void UpdateVisual(){
		if(ControllingPlayer == null){
			Color = Game.CLEAR;
			//outline.DefaultColor = Colors.Black;
		}else{
			Color = new Color(ControllingPlayer.PlayerColor,0.8f);
			SFX.Play("Beep",1,GlobalPosition);
			//outline.DefaultColor = Colors.White;
		}
	}
}