using System;
using System.Collections.Generic;
using Godot;

public partial class TargetZone : Area2D{
	[Export]
	private int minPointValue = 10;
	[Export]
	private int maxPointValue = 10;
	private int pointValue = 10;
	private Label pointLabel;
	private Polygon2D zoneVisual;
	private Dictionary<Player,float> playersTimeInZone = new Dictionary<Player, float>();
	private const float SCORE_TIME = 1.75f; //How many seconds player must be in zone to get the points
	public override void _Ready(){
		pointLabel = GetNode<Label>("TargetCollision/Label");
		zoneVisual = new Polygon2D();
		Node2D collision = GetNode<Node2D>("TargetCollision");
		Line2D outline = new Line2D();
		outline.DefaultColor = Colors.Black;
		outline.Closed = true;
		outline.Position = collision.Position;
		switch(collision){
			case CollisionShape2D collisionShape:
				switch(collisionShape.Shape){
					case RectangleShape2D rectangleShape:
						Vector2 size = rectangleShape.GetRect().Size;
						Vector2[] points = {
							new Vector2(-size.X/2,-size.Y/2),
							new Vector2(size.X/2,-size.Y/2),
							new Vector2(size.X/2,size.Y/2),
							new Vector2(-size.X/2,size.Y/2)
						};
						zoneVisual.Polygon = points;
						outline.Points = points;
						Mode.AddCameraTarget(collision);
						break;
					default:
						GD.PrintErr(collisionShape.Shape.GetType() + " is not supported as a Target Zone collision");
						break;
				}
				break;
			default:
				GD.PrintErr("Target Zone Collision not found on " + Name);
				break;
		}
		(Mode.ModeNode as TargetTest).TargetZones.Add(this);
		if(minPointValue > maxPointValue) minPointValue = maxPointValue;
		pointLabel.Visible = true;
		pointLabel.PivotOffset = pointLabel.Size / 2;
		pointLabel.GlobalPosition -= new Vector2(0,pointLabel.Size.Y/2);
		zoneVisual.Position = collision.Position;
		zoneVisual.Color = new Color(0,1,0,1);
		CanvasGroup canvasGroup = GetNode<CanvasGroup>("CanvasGroup");
		canvasGroup.SelfModulate = new Color(1,1,1,0.5f);
		canvasGroup.AddChild(zoneVisual);
		canvasGroup.MoveChild(zoneVisual,0);
		pointLabel.Reparent(zoneVisual);
		AddChild(outline);
		canvasGroup.ZIndex--;
		UpdatePointValue();
		if(!Online.IsHost()) SetPhysicsProcess(false);
	}

	public override void _PhysicsProcess(double delta){
		if(Online.IsHost()){
			float fDelta = (float)delta;
			foreach(KeyValuePair<Player,float> playerTime in playersTimeInZone){
				Player player = playerTime.Key;
				playersTimeInZone[player] += fDelta;
				if(playersTimeInZone[player] >= SCORE_TIME){
					Rpc(nameof(PlayerScored),player.Id,pointValue,player.Rb.GlobalPosition);
					Death.KillPlayer(player,Death.DeathCause.Respawn);
					UpdatePointValue();
					if(TargetTest.PlayerScores[player.Id-1] > TargetTest.TotalScore){
						Mode.GameFinished();
					}
				}
			}
		}
	}

	public void _on_body_entered(PhysicsBody2D body){
		if(Online.IsHost() && body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			playersTimeInZone.Add(player,0);
		}
	}

	public void _on_body_exited(PhysicsBody2D body){
		if(Online.IsHost() && body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			playersTimeInZone.Remove(player);
		}
	}

	public void UpdatePointValue(){
		pointValue = GetRandomPointValue(); //5 point intervals
		byte[] shortPointValue = BitConverter.GetBytes((short)pointValue);
		Rpc(nameof(SyncPointValue),shortPointValue);
		pointLabel.Text = pointValue.ToString();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = false,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SyncPointValue(byte[] pointValueArray){
		pointValue = BitConverter.ToInt16(pointValueArray, 0);
		pointLabel.Text = pointValue.ToString();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void PlayerScored(byte id, int score, Vector2 position){
		SFX.Play("Lap",position);
		TargetTest.PlayerScores[id-1] += score;
		if(TargetTest.PlayerScores[id-1] > TargetTest.TopScore){
			TargetTest.TopScore = TargetTest.PlayerScores[id - 1];
		}
	}

	private int GetRandomPointValue(){
		//Gets value in min max range at intervals of 5
		int minMultiplier = (int)Math.Ceiling(minPointValue / 5f);
		int maxMultiplier = (int)Math.Floor(maxPointValue / 5f);
		int randomMultiplier = Random.Shared.Next(minMultiplier, maxMultiplier + 1);
		return randomMultiplier * 5;
	}
}