using Godot;
using System;

public partial class Trail : Node{
	public RigidBody2D PlayerRb;
	public const int TRAIL_LENGTH = 16;
	private Line2D trailLine;
	
	public override void _Ready(){
		trailLine = GetNode<Line2D>("TrailLine");
	}

	public override void _PhysicsProcess(double delta){
		AddPoint(PlayerRb.GlobalPosition);
	}

	public void AddPoint(Vector2 point){
		if(!Level.IsPositionOffscreenOrDead(PlayerRb.GlobalPosition)){
			trailLine.AddPoint(point);
			if(trailLine.GetPointCount() > TRAIL_LENGTH){
				trailLine.RemovePoint(0);
			}
		}else if(trailLine.Points.Length > 0){
			//Still remove oldest
			trailLine.RemovePoint(0);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ResetTrail(){
		trailLine.Points = Array.Empty<Vector2>();
	}

	public Vector2 GetPreviousPosition(int ticksAgo){
		//If there is no previous points to go back to return infinity vector
		if(trailLine.Points.Length == 0) return Vector2.Inf;
		//If ticks ago is to far back get oldest position
		if(ticksAgo > trailLine.Points.Length) ticksAgo = trailLine.Points.Length;
		//If for some reason negative set to 0
		else if(ticksAgo < 1) ticksAgo = 1;
		//Return the position from said ticks ago
		return trailLine.Points[trailLine.Points.Length - ticksAgo];
	}

	public void SlicePoints(int ticks){
		int length = trailLine.Points.Length - ticks;
		if(length < 0) trailLine.Points = Array.Empty<Vector2>();
		//else trailLine.Points = trailLine.Points.ToList().Slice(0,length).ToArray();
		else Array.Copy(trailLine.Points, trailLine.Points,length);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)Online.TransferChannelEnum.Trail)]
	public void SyncTrail(Vector2[] points){
		foreach(Vector2 point in points) AddPoint(point);
	}
}