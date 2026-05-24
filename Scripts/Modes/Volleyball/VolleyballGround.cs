using Godot;

public partial class VolleyballGround : Area2D{
	[Export]
	private string team;
	
	public void _on_area_2d_body_entered(PhysicsBody2D body) {
        if(Online.IsHost() && body.IsInGroup("Ball")){
			(Mode.ModeNode as TeamSportsMode).Rpc(nameof(TeamSportsMode.PointScored),team);
        }else if(body.IsInGroup("Player")){
            Player player = body.GetParent() as Player;
            if(!player.Team.Equals(team) && Online.IsHost() || player.TicksToIgnore != 0){
                Death.KillPlayer(player,Death.DeathCause.Pop);
            }
		}
    }
}