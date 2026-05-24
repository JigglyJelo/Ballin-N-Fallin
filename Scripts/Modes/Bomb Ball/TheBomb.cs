using Godot;

public partial class TheBomb : SportBall{
	public Label BombTimerText;
	public const float BOMB_TIME = 15;
	public float TeamABombTimer = BOMB_TIME;
	public float TeamBBombTimer = BOMB_TIME;
	private Node2D rightSideUpNode;
	public override void _Ready(){
		Rb = GetNode<InterpolatedBody>("Ball");
		Smoother = GetNode<Node2D>("Smoothing2D");
		BombBall.SetBombTimers();
		rightSideUpNode = GetNode<Node2D>("Smoothing2D/Sprites/RightsideUp");
		BombTimerText = GetNode<Label>("Smoothing2D/Sprites/RightsideUp/Label");
		Mode.AddCameraTarget(Smoother);
	}

    public override void _Process(double delta){
		rightSideUpNode.GlobalRotation = 0;
    }

	public override void _PhysicsProcess(double delta){
		base._PhysicsProcess(delta);
		if(Rb.GlobalPosition.X < -1){
			TeamABombTimer -= (float)delta;
			if(TeamABombTimer <= 0){
				if(Online.IsHost()){
					ParticleManager.ParticleManagerNode.Rpc(nameof(ParticleManager.ParticleManagerNode.SpawnExplosion), Rb.GlobalPosition);
					(Mode.ModeNode as TeamSportsMode).Rpc(nameof(TeamSportsMode.PointScored),"A");
				}
			}else{
				string bombTimerString = float.Ceiling(TeamABombTimer).ToString();
				if(!bombTimerString.Equals(BombTimerText.Text)){
					BombTimerText.Text = bombTimerString;
				}
				Color teamAColor = (Mode.ModeNode as TeamMode).TeamAColor;
				if(BombTimerText.SelfModulate != teamAColor){
					BombTimerText.SelfModulate = teamAColor;
				}
			}
		}else if(Rb.GlobalPosition.X > 1){
			TeamBBombTimer -= (float)delta;
			if(TeamBBombTimer <= 0){
				if(Online.IsHost()){
					ParticleManager.ParticleManagerNode.Rpc(nameof(ParticleManager.ParticleManagerNode.SpawnExplosion), Rb.GlobalPosition);
					(Mode.ModeNode as TeamSportsMode).Rpc(nameof(TeamSportsMode.PointScored),"B");
				}
			}else{
				string bombTimerString = float.Ceiling(TeamBBombTimer).ToString();
				if(!bombTimerString.Equals(BombTimerText.Text)){
					BombTimerText.Text = bombTimerString;
				}
				Color teamBColor = (Mode.ModeNode as TeamMode).TeamBColor;
				if(BombTimerText.SelfModulate != teamBColor){
					BombTimerText.SelfModulate = teamBColor;
				}
			}
		}
	}
}