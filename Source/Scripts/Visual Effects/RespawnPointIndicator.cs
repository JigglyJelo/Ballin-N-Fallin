using Godot;

public partial class RespawnPointIndicator : TextureProgressBar{
	public Player Player;
	public override void _Ready(){
		Value = 0;
		TintProgress = Player.PlayerColor;
		GlobalPosition = Player.SpawnPoint;
	}

	public override void _PhysicsProcess(double delta){
		Value += delta*MaxValue;
		if(Value >= MaxValue){
			if(Online.IsHost()) Player.Rpc(nameof(Player.RespawnPlayer));
			QueueFree();
		}
	}
}