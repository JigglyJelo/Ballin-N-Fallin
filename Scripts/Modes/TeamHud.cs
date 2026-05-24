using Godot;

public partial class TeamHud : CanvasLayer{
	private Label teamALabel,teamBLabel,goalText;
	private bool setColors = false;
	public override void _Ready(){
		Scale = Game.ContentScaleVector2;
		teamALabel = GetNode<Label>("ATeamText");
		teamBLabel = GetNode<Label>("BTeamText");
		goalText = GetNode<Label>("GoalText");
		goalText.Text = "First to " + TeamSportsMode.TotalScore;
	}

	public override void _PhysicsProcess(double delta){
		if(!setColors && TeamSportsMode.Teams.Length != 0){
			SetHUDColors();
		}
	}

	public void SetHUDColors(){
		if(Game.TotalPlayers != 2){
            teamALabel.SelfModulate = Game.TeamColors[0];
            teamBLabel.SelfModulate = Game.TeamColors[1];
        }else{
            if(TeamSportsMode.Teams[0].Equals("A")){
                teamALabel.SelfModulate = Game.Players[0].PlayerColor;
                teamBLabel.SelfModulate = Game.Players[1].PlayerColor;
            }else{
                teamALabel.SelfModulate = Game.Players[1].PlayerColor;
                teamBLabel.SelfModulate = Game.Players[0].PlayerColor;
            }
        }
		setColors = true;
		SetPhysicsProcess(false);
	}
}
