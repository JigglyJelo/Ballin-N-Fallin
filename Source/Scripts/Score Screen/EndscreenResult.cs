using Godot;
using System;
using System.Text;

public partial class EndscreenResult : Node2D{
	const int BAR_FULL_HEIGHT = -768;
	public int Id;
	private CollisionPolygon2D collisionPolygon;
	private Polygon2D visualPolygon;
	private Line2D barOutline;
	private Label roundResultText,scoreText;
	private int scoreIncreaseAmount;
	private ScoreScreen scoreScreen;
	private bool tweeningBar = false;
	public ScoreScreenPlayer ScorePlayer;
	private Color color;
	private int scoreIndex;
	
	public async override void _Ready(){
		scoreIndex = Id-1;
		color = Game.PlayerDatas[Id-1].PlayerColor;
		ScorePlayer = GetNode<ScoreScreenPlayer>("Player");
		ScorePlayer.Id = Id;
		scoreScreen = GetParent() as ScoreScreen;
		SetPhysicsProcess(true);
		roundResultText = GetNode<Label>("RoundResult");
		roundResultText.SelfModulate = color;
		scoreText = GetNode<Label>("PlayerScore");
		scoreText.SelfModulate = color;
		visualPolygon = GetNode<Polygon2D>("Bar");
		barOutline = GetNode<Line2D>("Bar/BarOutline");
		visualPolygon.Color = color;
		StaticBody2D staticBody2D = new StaticBody2D();
		AddChild(staticBody2D);
		collisionPolygon = new CollisionPolygon2D();
		collisionPolygon.Polygon = visualPolygon.Polygon;
		collisionPolygon.Position = new Vector2(visualPolygon.Position.X,visualPolygon.Position.Y-20);
		staticBody2D.AddChild(collisionPolygon);
		byte roundPlacement = Mode.Positions[Id-1];
		GD.Print("Placement: " + roundPlacement);
		if(ScoreScreen.TourFinished) scoreIncreaseAmount = Tour.PlayerScores[Id-1];
		else scoreIncreaseAmount = Tour.GetIncreaseAmount(roundPlacement);
		SetInitialBarHeight();
		if(!Tour.IsTour){
			scoreText.Text = Game.GetUsername(Id);
		}else{
			scoreText.Text = "Score: " + (Tour.PlayerScores[scoreIndex]-scoreIncreaseAmount) + "\n"+Game.GetUsername(Id);
		}
		
		//Set text
		StringBuilder resultTextBuilder = new StringBuilder();
		switch(roundPlacement){
			case 1: resultTextBuilder.Append(" 1st\n"); break;
			case 2: resultTextBuilder.Append(" 2nd\n"); break;
			case 3: resultTextBuilder.Append(" 3rd\n"); break;
			default: 
				resultTextBuilder.Append(" "); 
				resultTextBuilder.Append(roundPlacement);
				resultTextBuilder.Append("th\n");
				break;
		}
		if(Tour.IsTour && !ScoreScreen.TourFinished){
			resultTextBuilder.Append("+");
			resultTextBuilder.Append(scoreIncreaseAmount);
			resultTextBuilder.Append("\n ");
		}
		resultTextBuilder.Append(getModeResultText());
		roundResultText.Text = resultTextBuilder.ToString();

		string getModeResultText(){
			switch(Game.CurrentMode){
				case Mode.GameMode.Race:
					TimeSpan raceTime = new TimeSpan();
					if(Race.Scores[Id-1] != float.MaxValue) raceTime = TimeSpan.FromSeconds(Race.Scores[Id-1]);
					return (Race.Scores[Id-1] != float.MaxValue) ? "Time:\n" + raceTime.ToString("m':'ss':'fff") : "Did not finish";
				case Mode.GameMode.Deathmatch:
					TimeSpan deathTime = new TimeSpan();
					if(roundPlacement != 1 && Mode.Scores[Id-1] != float.MinValue && Mode.Scores[Id-1] != float.MaxValue) deathTime = TimeSpan.FromSeconds(Mode.Scores[Id-1]);
					return (roundPlacement == 1) ? "Lived" : "Died after:\n" + deathTime.ToString("m':'ss':'fff");
				case Mode.GameMode.Golf:
					return "Strokes: " + Golf.Scores[Id-1];
				case Mode.GameMode.KingOfTheHill:
					return "Score: " + KOTH.Scores[Id-1] + "/" + KOTH.TotalScore;
				case Mode.GameMode.CrownTheKing:
					return "Score: " + CTK.Scores[Id-1] + "/" + CTK.TotalScore;
				case Mode.GameMode.Survival:
					TimeSpan survivalTime = new TimeSpan();
					if(roundPlacement != 1 && Mode.Scores[Id-1] != float.MinValue && Mode.Scores[Id-1] != float.MaxValue) survivalTime = TimeSpan.FromSeconds(Mode.Scores[Id-1]);
					return (roundPlacement == 1) ? "Survived" : "Survived for\n" + survivalTime.ToString("m':'ss':'fff");
				case Mode.GameMode.BallinToTheBank:
					return "Deposited\n$" + BTTB.DepositedMoney[Id-1] + " / $" + BTTB.MoneyToWin;
				case Mode.GameMode.Domination:
					return "Score: "+ (int)Domination.Scores[Id-1]+"/"+(int)Domination.TotalScore;
				case Mode.GameMode.TargetTest:
					return "Score: " + TargetTest.PlayerScores[Id-1] + "/" + TargetTest.TotalScore;
				default:
					return "";
			}
		}

		await ToSignal(GetTree().CreateTimer(0.25,true), "timeout");
		UpdateResults();
	}

    public override void _Process(double delta){
		if(tweeningBar){
			BarHeightTween((float)delta);
		}
	}

    public override void _PhysicsProcess(double delta){
        ScorePlayer.GlobalPosition = new Vector2(GlobalPosition.X+192,ScorePlayer.GlobalPosition.Y); //Find a way to not to this every tick
    }

    private async void UpdateResults(){
		await ToSignal(GetTree().CreateTimer(1.0/3.0,true), "timeout");
		Tween roundTextTween = GetTree().CreateTween();
		roundTextTween.TweenProperty(roundResultText,"self_modulate",Game.CLEAR,1.5);
		tweeningBar = true;
		if(!visualPolygon.Visible && Tour.PlayerScores[scoreIndex] != 0){
			visualPolygon.Visible = true;
			barOutline.Visible = true;
		}else if(Tour.PlayerScores[scoreIndex] == 0 && !visualPolygon.Visible && scoreIncreaseAmount == 0){
			collisionPolygon.Polygon = Array.Empty<Vector2>();
		}
	}

	private void BarTweenEnded(){
		tweeningBar = false;
		if(Tour.IsTour) scoreText.Text = "Score: " + Tour.PlayerScores[scoreIndex] + "\n"+Game.GetUsername(Id);
	}

	private async void BarHeightTween(float delta){
		float endScale;
		if(Tour.IsTour){
			endScale = Tour.PlayerScores[scoreIndex]/(float)Tour.TotalScore;
		}else{
			endScale = scoreIncreaseAmount/10f;
		}
		float tweenSpeed;
		if(scoreIncreaseAmount != 0){
			if(ScoreScreen.TourFinished){
				tweenSpeed = (scoreIncreaseAmount / (float)Tour.TotalScore) * 3.225f;
			}else{
				tweenSpeed = scoreIncreaseAmount / (Tour.IsTour ? 10f : 30f);
			}
		}else{
			tweenSpeed = 100;
		}
		if(visualPolygon.Polygon[0].Y > BAR_FULL_HEIGHT*endScale){
			//Raise BarHeight
			float newHeight = (BAR_FULL_HEIGHT*endScale)*(delta/tweenSpeed);
			Vector2[] polygon = visualPolygon.Polygon;
			polygon[0].Y += newHeight;
			polygon[1].Y += newHeight;
			visualPolygon.Polygon = polygon;
			collisionPolygon.Polygon = polygon;
			Vector2[] points = barOutline.Points;
			points[0].Y += newHeight;
			points[1].Y += newHeight;
			barOutline.Points = points;
			//Increment Score Text if needed
			int displayedScoreValue = (int)((visualPolygon.Polygon[0].Y/-768)*Tour.TotalScore);
        	if(Tour.IsTour && !scoreText.Text.Equals("Score: " + displayedScoreValue + "\n"+Game.GetUsername(Id))){
				scoreText.Text = "Score: " + displayedScoreValue + "\n"+Game.GetUsername(Id);
			}
		}else if(visualPolygon.Polygon[0].Y <= BAR_FULL_HEIGHT*endScale){
			tweeningBar = false;
			if(Tour.IsTour){
				int finalPlacement = -1;
				
				int[] sortedScores = new int[Tour.PlayerScores.Length];
				for(int i = 0; i < Tour.PlayerScores.Length; i++) sortedScores[i] = Tour.PlayerScores[i];
				Array.Sort(sortedScores);
				
				for(int i = 0; i < sortedScores.Length; i++){
					if(sortedScores[i] == Tour.PlayerScores[scoreIndex]){
						finalPlacement = Tour.PlayerScores.Length-i;
						break;
					}
				}
				
				
				if(ScoreScreen.TourFinished){
					if(finalPlacement == 1){

					}else if(finalPlacement == Game.TotalPlayers){
						ScorePlayer.EyeSprite.Texture = Player.GetEyeTexture(Player.Emotion.Sad,false);
					}
				}
				scoreText.Text = "Score: " + Tour.PlayerScores[scoreIndex] + "\n"+Game.GetUsername(Id);
			} 
			await ToSignal(GetTree().CreateTimer(0.7,true), "timeout");
			scoreScreen.EndScreenDone();
		}
	}

	private void SetInitialBarHeight(){
		int startingScore;
		if(ScoreScreen.TourFinished){
			startingScore = 0;
		}else{
			startingScore = Tour.PlayerScores[scoreIndex]-scoreIncreaseAmount;
		}
		if(startingScore == 0){
			visualPolygon.Visible = false;
			barOutline.Visible = false;
		}
		
		float startingHeight = BAR_FULL_HEIGHT*(startingScore/(float)Tour.TotalScore);
		GD.Print(startingHeight);
		Vector2[] polygon = visualPolygon.Polygon;
		polygon[0].Y = startingHeight;
		polygon[1].Y = startingHeight;
		
		visualPolygon.Polygon = polygon;
		collisionPolygon.Polygon = visualPolygon.Polygon;
		Vector2[] points = barOutline.Points;
		points[0].Y = startingHeight;
		points[1].Y = startingHeight;
		barOutline.Points = points;
	}
}