using Godot;
using System;
using System.Collections.Generic;

public partial class PayloadTower : Node2D{
	private Area2D zone;
	private Line2D payloadPath;
	private Polygon2D areaPolygon, groundPolygon, floorPolygon;
	private List<Player> playersInZone = new List<Player>();
	private byte[] distanceData = new byte[6];
	private Color teamAColor,teamBColor;
	public float Distance;
	private const float SPEED = 300;
	private float lineLength;

	public override void _Ready(){
		zone = GetNode<Area2D>("Physics/Area2D");
		payloadPath = GetParent().GetNode<Line2D>("PayloadPath");
		lineLength = getLine2DLength(payloadPath);
		areaPolygon = GetNode<Polygon2D>("Visual/Zone");
		groundPolygon = GetNode<Polygon2D>("Visual/Platform");
		floorPolygon = GetNode<Polygon2D>("Visual/Floor");
		Distance = 0.5f;
		Position = GetPositionAlongLine(0.5f);
		if(Game.TotalPlayers != 2){
			teamAColor = Game.TeamColors[0];
			teamBColor = Game.TeamColors[1];
		}else{
			if(Game.Players[0].Team.Equals("A")){
				teamAColor = Game.Players[0].PlayerColor;
				teamBColor = Game.Players[1].PlayerColor;
			}else if(Game.Players[0].Team.Equals("B")){
				teamAColor = Game.Players[1].PlayerColor;
				teamBColor = Game.Players[0].PlayerColor;
			}
		}
		Mode.AddCameraTarget(areaPolygon);

		float getLine2DLength(Line2D line){
    		float totalLength = 0;
    		for(int i = 0; i < line.Points.Length - 1; i++){
    		    totalLength += line.Points[i].DistanceTo(line.Points[i + 1]);
    		}
    		return totalLength;
		}
	}

	public override void _PhysicsProcess(double delta){
		if(Online.IsHost()){
			float fDelta = (float)delta;
			int teamAPlayers = 0;
			int teamBPlayers = 0;
			foreach(Player player in playersInZone){
				if(player.Team.Equals("A")) teamAPlayers++;
				else teamBPlayers++;
			}

			if(teamAPlayers > teamBPlayers){
				float playersWeight = (float)teamAPlayers/TeamMode.TeamAPlayerCount;
				IncrementDistance(fDelta*playersWeight);
			}else if(teamAPlayers < teamBPlayers){
				float playersWeight = (float)teamBPlayers/TeamMode.TeamBPlayerCount;
				IncrementDistance(-fDelta*playersWeight);
			}else if(teamAPlayers == 0 && teamBPlayers == 0){
				const float RETURN_SPEED = 4;
				if(Distance > 0.5f){
					IncrementDistance(-fDelta/RETURN_SPEED);
				}else if(Distance < 0.5f){
					IncrementDistance(fDelta/RETURN_SPEED);
				}
			}
			//Sync distance online
			if(Online.IsOnline){
				BitConverter.GetBytes(Distance).CopyTo(distanceData, 0);
				ushort distanceUpdate = UnreliableManager.GetChannelLastUpdate(UnreliableManager.UnreliableChannel.Payload);
				BitConverter.GetBytes(distanceUpdate).CopyTo(distanceData, distanceData.Length-2);
				Rpc(nameof(SyncDistance),distanceData);
			}
		}
	}

	public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			playersInZone.Add(player);
			ColorUpdate();
		}
	}

	public void _on_area_2d_body_exited(PhysicsBody2D body){
		if(body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			playersInZone.Remove(player);
			ColorUpdate();
		}
	}

	public void IncrementDistance(float delta){
		float distanceIncreased = SPEED * delta;
		Distance += distanceIncreased/lineLength;
		DistanceChecks();
	}

	public void DistanceChecks(){
		if(Distance >= 1){
			TeamMode.WinningTeam = "A";
			if(Online.IsHost()) Mode.GameFinished();
		}else if(Distance <= 0){
			TeamMode.WinningTeam = "B";
			if(Online.IsHost()) Mode.GameFinished();
		}else{
			Position = GetPositionAlongLine(Distance);
		}
	}

	private void ColorUpdate(){
		int teamAPlayers = 0;
		int teamBPlayers = 0;
		foreach(Player player in playersInZone){
			if(player.Team.Equals("A")) teamAPlayers++;
			else teamBPlayers++;
		}
		if(teamAPlayers == teamBPlayers){
			areaPolygon.Color = new Color(1,1,1,0.5f);
			groundPolygon.Color = new Color(0.85f, 0.85f, 0.85f);
			floorPolygon.Color = Colors.White;
		}else if(teamAPlayers > teamBPlayers){
			float playersWeight = (float)teamAPlayers/TeamMode.TeamAPlayerCount;
			Color color = new Color(teamAColor.R*playersWeight,teamAColor.G*playersWeight,teamAColor.B*playersWeight);
			areaPolygon.Color = new Color(color,0.5f);
			groundPolygon.Color = color;
			floorPolygon.Color = new Color(Mathf.Min(color.R + 0.15f, 1), Mathf.Min(color.G + 0.15f, 1), Mathf.Min(color.B + 0.15f, 1));
		}else{
			float playersWeight = (float)teamBPlayers/TeamMode.TeamBPlayerCount;
			Color color = new Color(teamBColor.R*playersWeight,teamBColor.G*playersWeight,teamBColor.B*playersWeight);
			areaPolygon.Color = new Color(color,0.5f);
			groundPolygon.Color = color;
			floorPolygon.Color = new Color(Mathf.Min(color.R + 0.15f, 1), Mathf.Min(color.G + 0.15f, 1), Mathf.Min(color.B + 0.15f, 1));
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = false,TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void SyncDistance(byte[] distanceData){ //First four bytes distance as float last 2 ushort update
		ushort update = BitConverter.ToUInt16(distanceData, distanceData.Length-2);
		if(UnreliableManager.IsNewerRpc(UnreliableManager.UnreliableChannel.Payload,update)){
			Distance = BitConverter.ToSingle(distanceData,0);
			DistanceChecks();
		}
	}

	public Vector2 GetPositionAlongLine(float distance){
        distance = Mathf.Clamp(distance, 0f, 1f);
        Vector2[] points = payloadPath.Points;

        if (points.Length < 2)
            return points.Length == 1 ? points[0] : Vector2.Zero;

        // Step 1: Compute total length
        float totalLength = 0f;
        List<float> segmentLengths = new();
        for (int i = 0; i < points.Length - 1; i++)
        {
            float length = points[i].DistanceTo(points[i + 1]);
            segmentLengths.Add(length);
            totalLength += length;
        }

        float targetDistance = totalLength * distance;
        float accumulated = 0f;

        // Step 2: Walk through segments
        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segmentLength = segmentLengths[i];

            if (accumulated + segmentLength >= targetDistance)
            {
                float segmentT = (targetDistance - accumulated) / segmentLength;
                return points[i].Lerp(points[i + 1], segmentT);
            }

            accumulated += segmentLength;
        }

        // Fallback in case t = 1.0 exactly
        return points[^1];
    }
}