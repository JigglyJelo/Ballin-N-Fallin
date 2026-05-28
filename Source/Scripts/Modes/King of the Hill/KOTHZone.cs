using Godot;
using System.Collections.Generic;

public partial class KOTHZone : Area2D{
	private List<Player> playersInZone = new List<Player>();
	private readonly Color DEFAULT_COLOR = new Color(1,1,1,0.5f);
	private Polygon2D visualPolygon;
	private Line2D line;
	private bool lineGradientIncreasing = true;

    public override void _Ready(){
        visualPolygon = new Polygon2D();
		CollisionPolygon2D areaPolygon = GetNode<CollisionPolygon2D>("CollisionPolygon2D");
		visualPolygon.Polygon = areaPolygon.Polygon;
		visualPolygon.Position = areaPolygon.Position;
		visualPolygon.Color = DEFAULT_COLOR;
		visualPolygon.TextureRepeat = TextureRepeatEnum.Enabled;
		visualPolygon.Texture = GD.Load<Texture2D>("res://Assets/Sprites/Level Stuff/Ground Patterns/Golf Tile.png");
		visualPolygon.ZIndex -= 3;
		AddChild(visualPolygon);
		line = new Line2D();
		line.Points = areaPolygon.Polygon;
		line.Position = areaPolygon.Position;
		line.DefaultColor = Colors.Black;
		line.Closed = true;
		line.ZIndex -= 2;
		AddChild(line);
    }

    public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(body.GetParent().IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			playersInZone.Add(player);
			if(player.PlayerEmotion == Player.Emotion.Annoyed) player.PlayerEmotion = Player.Emotion.Neutral;
			if(playersInZone.Count == 1){
				visualPolygon.Color = new Color(playersInZone[0].PlayerColor,0.8f);
			}else{
				visualPolygon.Color = DEFAULT_COLOR;
			}
		}
	}

	public void _on_area_2d_body_exited(PhysicsBody2D body){
    	if(body.GetParent().IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			playersInZone.Remove(player);
			float timeToLose;
			switch(Game.TotalPlayers){
    			case 2:
        			timeToLose = 5;
        			break;
    			case 3:
					timeToLose = 3;
					break;
				case 4:
        			timeToLose = 2.25f;
        			break;
    			case 5:
					timeToLose = 2;
					break;
    			case 6:
    			    timeToLose = 1.75f;
    			    break;
    			default:
    			    timeToLose = 1.5f;
    			    break;
			}
			if(player.Score >= KOTH.TotalScore - timeToLose){
				player.Score = KOTH.TotalScore - timeToLose;
				player.PlayerEmotion = Player.Emotion.Angry;
			}else player.PlayerEmotion = Player.Emotion.Annoyed;


			player.Visuals.ShowPlayerText();
			if(MusicPlayer.GetPitch() == KOTH.FAST_MUSIC_SPEED && !KOTH.Finished){
				bool resetPitch = true;
				foreach(Player otherPlayer in Game.Players){
					if(otherPlayer != player && playersInZone.Contains(otherPlayer) && otherPlayer.Score >= KOTH.TotalScore*0.75f){
						resetPitch = false;
						break;
					}else if(otherPlayer.Score >= KOTH.TotalScore - timeToLose){
						resetPitch = false;
						break;
					}
				}
				if(resetPitch) MusicPlayer.SetPitch(1);
			}
			if(playersInZone.Count == 1){
				visualPolygon.Color = new Color(playersInZone[0].PlayerColor,0.8f);
			}else{
				visualPolygon.Color = DEFAULT_COLOR;
			}
		}
	}

    public override void _PhysicsProcess(double delta){
		if((playersInZone.Count == 1) || (playersInZone.Count == 2 && Game.TotalPlayers > 5)){
			foreach(Player player in playersInZone){
				if(player.Score >= KOTH.TotalScore && Online.IsHost()){
					Mode.GameFinished();
				}else{
					player.Score += (float)delta;
					player.Visuals.ShowPlayerText();
					if(player.Score > KOTH.TopScore) KOTH.TopScore = player.Score;
					if(player.Score >= KOTH.TotalScore*0.75f && MusicPlayer.GetPitch() != KOTH.FAST_MUSIC_SPEED){
						MusicPlayer.SetPitch(KOTH.FAST_MUSIC_SPEED);
					}
				}
			}
		}
    }

    public override void _Process(double delta){
		if(playersInZone.Count == 1){
			float speed = 100 * (float)delta;
			visualPolygon.TextureOffset -= new Vector2(speed,speed);
		}
	}
}