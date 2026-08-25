using Godot;

public partial class RespawnPointIndicator : Node2D{
	public Player Player;
	private TextureProgressBar progressBar;
	public override void _Ready(){
		progressBar = GetNode<TextureProgressBar>("ProgressBar");
		progressBar.Value = 0;
		progressBar.TintProgress = Player.PlayerColor;
		GlobalPosition = Player.SpawnPoint;
		Mode.AddCameraTarget(this);
	}

	public override void _PhysicsProcess(double delta){
		progressBar.Value += delta*progressBar.MaxValue;
		if(progressBar.Value >= progressBar.MaxValue){
			if(Online.IsHost()) Player.Rpc(nameof(Player.RespawnPlayer));
			QueueFree();
		}
	}
}