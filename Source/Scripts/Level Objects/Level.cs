using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


public partial class Level : Node2D {
    [Export]
    public sbyte LevelUnit; //Laps for Race or Par for Golf (Par should be set based on an itemless hole)
    [Export]
    public float CameraZoom = 1;
    [Export]
    private Color floorColorOverride = Game.ZEROES;
    [Export]
    public Color InsideColorOverride = Game.ZEROES;
    [Export]
    public Color OutlineColorOverride = Game.ZEROES;
    [Export]
    private Texture2D groundTexture;
    [Export]
    private PackedScene background;
    private static List<Node2D> respawnPoints;
    private static List<Node2D> spawnPoints;
    public const float OUTLINE_WIDTH = 9;
    private const float BAKE_INTERVAL = 50;
    public static Level LevelNode;

    public override void _Ready(){
        Game.DisableProcesses(this);
        LevelNode = this;
        //Set Camera Zoom
        Game.UpdateContentScaleVector();
        Game.Camera.Zoom = Game.ContentScaleVector2 * CameraZoom;
        CanvasLayer backgroundLayer = Game.GameNode.GetNode<CanvasLayer>("BackgroundLayer");
        backgroundLayer.Scale = Game.ContentScaleVector2;
        //Get level colors ready
        if(floorColorOverride.Equals(Game.ZEROES)) floorColorOverride = Mode.LevelPalette.FloorColor;
        if(InsideColorOverride.Equals(Game.ZEROES)) InsideColorOverride = Mode.LevelPalette.InsideColor;
        if(OutlineColorOverride.Equals(Game.ZEROES)) OutlineColorOverride = Mode.LevelPalette.OutlineColor;
        //Scale Background to right size
        if(groundTexture == null) groundTexture = GD.Load<Texture2D>("res://Assets/Sprites/Level Stuff/Ground Patterns/" + Mode.EnumToString(Game.CurrentMode) + " Tile.png");
        LevelBackground levelBackground;
        if(background != null){
            levelBackground = background.Instantiate<LevelBackground>();
        }else{
            levelBackground = GD.Load<PackedScene>("res://Source/Scenes/Backgrounds/BackgroundTemplate.tscn").Instantiate<LevelBackground>();
        }
        AddChild(levelBackground);
        if(Game.GameNode.GetTree().Root.ContentScaleMode == Window.ContentScaleModeEnum.CanvasItems && Game.Resolution > DisplayServer.WindowGetSize().Y && Game.Resolution >= Game.BASE_RES){
            float scale = DisplayServer.WindowGetSize().Y / (float)Game.BASE_RES;
            levelBackground.Scale = new Vector2(scale,scale);
        }
        //Find the static bodies
        List<StaticBody2D> staticBodies = new List<StaticBody2D>();
        foreach(Node child in GetChildren()){
            if(child is StaticBody2D staticBody){
                staticBodies.Add(staticBody);
                staticBody.CollisionLayer = 0b11;
                if(staticBody.IsInGroup("NoRegain")){
                    staticBody.Modulate = new Color(0.85f,0.85f,0.85f);
                }
            }
        }

        //Creates all children visual Polygons for each CollisionPolygon2D that are children of static bodies
        foreach(StaticBody2D staticBody in staticBodies){
            foreach(Node child in staticBody.GetChildren()){
                if(child is Path2D path){
                    if(path.Curve.BakeInterval != BAKE_INTERVAL) path.Curve.BakeInterval = BAKE_INTERVAL;
                    CollisionPolygon2D pathCollisionPolygon = new CollisionPolygon2D();
                    Vector2[] polygon = path.Curve.GetBakedPoints();
                    for(int i = 0; i < polygon.Length; i++){
                        polygon[i] = path.Position + (path.Rotation == 0 ? polygon[i] : polygon[i].Rotated(path.Rotation));
                    }
                    pathCollisionPolygon.Name = path.Name + "Polygon";
                    pathCollisionPolygon.Polygon = polygon;
                    pathCollisionPolygon.ZIndex = path.ZIndex;
                    if(path.HasMeta("invert")){
                        pathCollisionPolygon.SetMeta("invert",(bool)path.GetMeta("invert"));
                    }
                    staticBody.AddChild(pathCollisionPolygon);
                    path.QueueFree();
                }
            }
        }

        List<CollisionPolygon2D>[] collisions = new List<CollisionPolygon2D>[staticBodies.Count];
        for(int i = 0; i < collisions.Length; i++){//foreach(StaticBody2D staticBody in staticBodies){
            collisions[i] = new List<CollisionPolygon2D>();
            StaticBody2D staticBody = staticBodies[i];
            foreach (Node child in staticBody.GetChildren()){
                if(child is CollisionPolygon2D collisionPolygon){
                    if(collisionPolygon.Position != Vector2.Zero){
                        Vector2[] newPolygon = (Vector2[])collisionPolygon.Polygon.Clone();
                        for(int j = 0; j < collisionPolygon.Polygon.Length; j++){
                            newPolygon[j] = collisionPolygon.Polygon[j] + collisionPolygon.Position;
                        }
                        collisionPolygon.Position = Vector2.Zero;
                        collisionPolygon.Polygon = newPolygon;
                    }
                    collisions[i].Add(collisionPolygon);
                    if(collisionPolygon.HasMeta("inverted") || collisionPolygon.HasMeta("Inverted") || collisionPolygon.HasMeta("Invert"))
                        GD.PrintErr("WRONG META NAME ASSIGNED TO " + collisionPolygon.Name);
                }
            }
        }
        for(int i = 0; i < collisions.Length; i++){
            List<CollisionPolygon2D> collisionPolygons = collisions[i];
            for(int j = 0; j < collisionPolygons.Count; j++){
                for(int k = collisionPolygons.Count - 1; k > j; k--){
                    if(collisionPolygons[j].HasMeta("invert") == collisionPolygons[k].HasMeta("invert")){
                        Vector2 duplicatePoint = Vector2.Inf;
                        Vector2 movedDupePoint = Vector2.Inf;
                        CollisionPolygon2D collision = collisionPolygons[j];
                        if(collision.HasMeta("invert") && (bool)collision.GetMeta("invert")){
                            duplicatePoint = findDuplicatePoint(collision.Polygon);
                            //Move the duplicate point slightly so Godot's merge polygon function doesn't break
                            if(duplicatePoint != Vector2.Inf){
                                Vector2[] points = (Vector2[])collision.Polygon.Clone();
                                //List<Vector2> points = collision.Polygon.ToList();
                                int dupeIndex = Array.IndexOf(points,duplicatePoint); //points.IndexOf(duplicatePoint);
                                for(int l = 0; l < 4; l++){
                                    //Try to move it slightly in each cardinal until we find which one works
                                    switch(l){
                                        case 0:
                                            movedDupePoint = duplicatePoint + Vector2.Right;
                                            break;
                                        case 1:
                                            movedDupePoint = duplicatePoint + Vector2.Left;
                                            break;
                                        case 2:
                                            movedDupePoint = duplicatePoint + Vector2.Up;
                                            break;
                                        case 3:
                                            movedDupePoint = duplicatePoint + Vector2.Down;
                                            break;
                                    }
                                    points[dupeIndex] = movedDupePoint;
                                    if(Geometry2D.DecomposePolygonInConvex(points).Count != 0) break;
                                    else GD.Print("Ignore the Convex Error above"); //Gives error right now try to ignore
                                }
                                collision.Polygon = points;
                            }
                        }
                        //Readd the duplicate point
                        Godot.Collections.Array<Vector2[]> mergedPolygons = Geometry2D.MergePolygons(collision.Polygon, collisionPolygons[k].Polygon);
                        if(mergedPolygons.Count == 1){
                            collision.Polygon = mergedPolygons[0];
                            if(duplicatePoint != Vector2.Inf && collision.HasMeta("invert") && (bool)collision.GetMeta("invert")){
                                Vector2[] points = (Vector2[])collision.Polygon.Clone();
                                //List<Vector2> points = collision.Polygon.ToList();
                                points[Array.IndexOf(points,movedDupePoint)] = duplicatePoint;
                                //Vector2[] polygon = points.ToArray();
                                //collision.Polygon = polygon;
                                collision.Polygon = points;
                                collision.SetMeta("invert",true);
                            }

                            collisionPolygons[k].Free();
                            collisionPolygons.RemoveAt(k);
                        }

                        Vector2 findDuplicatePoint(Vector2[] polygon){
                            for(int i = 0; i < polygon.Length; i++){
                                for(int j = i+1; j < polygon.Length; j++){
                                    if(polygon[i].IsEqualApprox(polygon[j])) return polygon[i];
                                }
                            }
                            return Vector2.Inf;
                        }
                    }
                }
            }
        }
        
        for(int i = 0; i < collisions.Length; i++){
            List<CollisionPolygon2D> collisionPolygons = collisions[i];
            foreach(CollisionPolygon2D collisionPolygon in collisionPolygons){
                bool invert = false;
                Vector2[] visualPolygon;
                float maxOuterDistance = 128;
                if(collisionPolygon.HasMeta("invert")){ // && (bool)collisionPolygon.GetMeta("invert")
                    invert = (bool)collisionPolygon.GetMeta("invert");
                    //Create Clip Polygon (Used to clip the collision polygon)
                    Vector2[] clipPolygon = new Vector2[] {
                        Vector2.Inf, //Top Left
                        new Vector2(float.NegativeInfinity,float.PositiveInfinity), //Top Right
                        new Vector2(float.NegativeInfinity,float.NegativeInfinity), //Bottom Right
                        new Vector2(float.PositiveInfinity,float.NegativeInfinity)  //Bottom Left
                    };
                    foreach(Vector2 point in collisionPolygon.Polygon){
                        if(point.X <= clipPolygon[0].X && point.Y <= clipPolygon[0].Y) clipPolygon[0] = point;
                        if(point.X >= clipPolygon[1].X && point.Y <= clipPolygon[1].Y) clipPolygon[1] = point;
                        if(point.X >= clipPolygon[2].X && point.Y >= clipPolygon[2].Y) clipPolygon[2] = point;
                        if(point.X <= clipPolygon[3].X && point.Y >= clipPolygon[3].Y) clipPolygon[3] = point;
                    }
                    Godot.Collections.Array<Vector2[]> clippedPolygons = Geometry2D.ClipPolygons(clipPolygon,collisionPolygon.Polygon);

                    Vector2[] clippedPolygonCorners = new Vector2[] {
                        Vector2.Inf,
                        new Vector2(float.NegativeInfinity,float.PositiveInfinity),
                        new Vector2(float.NegativeInfinity,float.NegativeInfinity),
                        new Vector2(float.PositiveInfinity,float.NegativeInfinity)
                    };

                    if(clippedPolygons.Count > 0){
                        visualPolygon = clippedPolygons[0];
                        foreach(Vector2 point in visualPolygon){
                            if(point.X <= clippedPolygonCorners[0].X && point.Y <= clippedPolygonCorners[0].Y) clippedPolygonCorners[0] = point;
                            if(point.X >= clippedPolygonCorners[1].X && point.Y <= clippedPolygonCorners[1].Y) clippedPolygonCorners[1] = point;
                            if(point.X >= clippedPolygonCorners[2].X && point.Y >= clippedPolygonCorners[2].Y) clippedPolygonCorners[2] = point;
                            if(point.X <= clippedPolygonCorners[3].X && point.Y >= clippedPolygonCorners[3].Y) clippedPolygonCorners[3] = point;
                        }
                    }else{
                        visualPolygon = collisionPolygon.Polygon;
                    }
                    float distance;
                    for(int j = 0; j < 4; j++){
                        for(int k = 0; k < 2; k++){
                            distance = MathF.Abs(clipPolygon[j][k] - clippedPolygonCorners[j][k]);
                            if(distance > maxOuterDistance){
                                maxOuterDistance = distance;
                            }
                        }
                    }
                }else{
                    visualPolygon = collisionPolygon.Polygon;
                }
                CollisionPolygon2D topCollision = new CollisionPolygon2D();
                Polygon2D topPolygon = new Polygon2D();
                topPolygon.Color = floorColorOverride;
                Polygon2D insidePolygon = new Polygon2D();
                insidePolygon.Color = InsideColorOverride;
                insidePolygon.Texture = groundTexture;
                insidePolygon.TextureRepeat = TextureRepeatEnum.Enabled;
                staticBodies[i].AddChild(topCollision);
                insidePolygon.Polygon = visualPolygon;
                Vector2[] topPolygonArr = visualPolygon;
                Vector2[] topCollisionArr;
                //Position top polygon
                for(int j = 0; j < topPolygonArr.Length; j++){
                    topPolygonArr[j] += new Vector2(0,-32f);
                }
                topPolygon.ZIndex = insidePolygon.ZIndex - 1;

                //Corner Detection to fix corner visuals
                //Boolean #1 whether point should go Next(true) or Before(false) Vector2 Boolean #2 whether it is an Above(true) point or Below(false)
                Dictionary<Vector2,Tuple<bool,bool>> newPoints = new Dictionary<Vector2, Tuple<bool,bool>>();
                for(int j = 0; j < topPolygonArr.Length; j++){
                    Vector2 point = topPolygonArr[j];
                    Vector2 previousPoint = j != 0 ? topPolygonArr[j-1] : topPolygonArr[topPolygonArr.Length-1];
                    Vector2 nextPoint = j != topPolygonArr.Length-1 ? topPolygonArr[j+1] : topPolygonArr[0];
                    bool hasPointBelow = true;
                    bool isNext = false;
                    //Check if previous point is above and to the right or left of current point
                    if(point.Y < previousPoint.Y && MathF.Abs(point.X - previousPoint.X) > 0.01f){
                        //Check if previous point and next point are in the same horizontal direction
                        if(point.X > previousPoint.X && point.X > nextPoint.X){
                            hasPointBelow = false;
                            isNext = false;
                        }else if(point.X < previousPoint.X && point.X < nextPoint.X){
                            hasPointBelow = false;
                            isNext = false;
                        } 
                    }else if(point.Y < nextPoint.Y && MathF.Abs(point.X - nextPoint.X) > 0.01f){
                        //Check if previous point and next point are in the same horizontal direction
                        if(point.X > nextPoint.X && point.X > previousPoint.X){ //MathF.Max(nextPoint.X,previousPoint.X);
                            hasPointBelow = false;
                            isNext = true;
                        }else if(point.X < nextPoint.X && point.X < previousPoint.X){
                            hasPointBelow = false;
                            isNext = true;
                        }
                    }
                    if(!hasPointBelow){
                        newPoints.Add(topPolygonArr[j],new Tuple<bool, bool>(isNext, false));
                        //GD.Print("corner none below");
                    }
                    bool hasPointAbove = true;
                    //Check if previous point is below and to the right or left of current point
                    if(point.Y > previousPoint.Y && MathF.Abs(point.X - previousPoint.X) > 0.01f){
                        //Check if previous point and next point are in the same horizontal direction
                        if(point.X > previousPoint.X && point.X > nextPoint.X){
                            hasPointAbove = false;
                            isNext = false;
                        }else if(point.X < previousPoint.X && point.X < nextPoint.X){
                            hasPointAbove = false;
                            isNext = false;
                        } 
                    }else if(point.Y > nextPoint.Y && MathF.Abs(point.X - nextPoint.X) > 0.01f){
                        //Check if previous point and next point are in the same horizontal direction
                        if(point.X > nextPoint.X && point.X > previousPoint.X){
                            hasPointAbove = false;
                            isNext = true;
                        }else if(point.X < nextPoint.X && point.X < previousPoint.X){
                            hasPointAbove = false;
                            isNext = true;
                        }
                    }
                    if(!hasPointAbove){
                        try{
                            newPoints.Add(topPolygonArr[j],new Tuple<bool, bool>(isNext, true));
                        }catch{ //(Exception ex)
                            //GD.Print(ex.ToString());
                        }
                        //GD.Print("corner none above");
                    }
                }
                List<Vector2> polygonPoints = new List<Vector2>();
                for(int j = 0; j < topPolygonArr.Length; j++){
                    polygonPoints.Add(topPolygonArr[j]);
                }
                foreach(Vector2 point in newPoints.Keys){
                    if(!newPoints[point].Item2){ //If bottom point
                        polygonPoints.Insert(polygonPoints.IndexOf(point) + (newPoints[point].Item1 ? 1 : 0),point + new Vector2(0,32f));
                    }else{ //If top point
                        polygonPoints.Insert(polygonPoints.IndexOf(point) + (newPoints[point].Item1 ? 0 : 1),point + new Vector2(0,32f));
                    }
                }
                topPolygonArr = polygonPoints.ToArray();
                topCollisionArr = (Vector2[])collisionPolygon.Polygon.Clone();
                for(int j = 0; j < topCollisionArr.Length; j++){
                    topCollisionArr[j] -= new Vector2(0,17f);
                }
                topPolygon.Polygon = topPolygonArr;
                topCollision.Position = collisionPolygon.Position;
                topCollision.Polygon = topCollisionArr;
                GDScript aaPolygonScript = (GD.Load("res://addons/antialiased_line2d/antialiased_polygon2d.gd") as GDScript);
                GodotObject aaInsidePolygon = (GodotObject)aaPolygonScript.New();
                Vector2[] insideArr = insidePolygon.Polygon;
                aaInsidePolygon.Call("set_polygon",insideArr);
                aaInsidePolygon.Set("texture",groundTexture);
                aaInsidePolygon.Set("texture_repeat",(int)TextureRepeatEnum.Enabled);
                aaInsidePolygon.Call("set_stroke_width",10);
                aaInsidePolygon.Call("set_stroke_color",OutlineColorOverride);
                aaInsidePolygon.Set("color",new Color(InsideColorOverride.R,InsideColorOverride.G,InsideColorOverride.B,InsideColorOverride.A));
                aaInsidePolygon.Set("z_index",insidePolygon.ZIndex);
                if(invert){
                    (aaInsidePolygon as Polygon2D).InvertEnabled = true;
                    (aaInsidePolygon as Polygon2D).InvertBorder = maxOuterDistance;
                }
                collisionPolygon.AddChild(aaInsidePolygon as Node);
                insidePolygon.Free();

                GodotObject aaTopPolygon = (GodotObject)aaPolygonScript.New();
                Vector2[] topArr = topPolygon.Polygon;
                aaTopPolygon.Call("set_polygon",topArr);
                aaTopPolygon.Call("set_stroke_width",10);
                aaTopPolygon.Call("set_stroke_color",OutlineColorOverride);
                aaTopPolygon.Set("color",new Color(floorColorOverride.R,floorColorOverride.G,floorColorOverride.B,floorColorOverride.A));
                aaTopPolygon.Set("z_index",topPolygon.ZIndex-1);
                (aaTopPolygon as Polygon2D).LightMask = 0b11;
                CallDeferred(nameof(SetDefferedLightmaskForLine), aaTopPolygon as Polygon2D);
                if(invert){
                    (aaTopPolygon as Polygon2D).InvertEnabled = true;
                    (aaTopPolygon as Polygon2D).InvertBorder = maxOuterDistance;
                }
                collisionPolygon.AddChild(aaTopPolygon as Node);
                topPolygon.Free();
            }
        }
        

        //Gathers and hides both spawns' and respawns' visuals
        spawnPoints = new List<Node2D>();
        respawnPoints = new List<Node2D>();
        foreach(Node node in GetChildren()){
            if(node.IsInGroup("Spawn")){
                Node2D spawnpoint = node as Node2D;
                spawnPoints.Add(spawnpoint);
                spawnpoint.Visible = false;
            }else if(node.IsInGroup("Respawn")){
                Sprite2D respawnPoint = node as Sprite2D;
                respawnPoint.Visible = false;
                respawnPoints.Add(node as Node2D);
            }
        }

        GetTree().Paused = true;
        //Make host set player spawnpoints
        if(Online.IsOnline){ //If not host spawn this immediately to prevent errors from not having the node when rpcs come
            Game.Players = Array.Empty<Player>();
            Mode.ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Players/PlayerSynchronizer.tscn").Instantiate());
        }
    }

    private void SetDefferedLightmaskForLine(Polygon2D polygon){
        polygon.GetChild<Node2D>(0).LightMask = 0b11;
    }

    private List<Polygon2D> levelCollisions = new List<Polygon2D>();
    private Vector2[] MergePolygons(Vector2[][] polygons){
        if(polygons.Length == 0){
            GD.PrintErr("No polygons to merge.");
            return Array.Empty<Vector2>();
        }

        Vector2[] mergedPolygon = polygons[0];

        for(int i = 1; i < polygons.Length; i++){
            Godot.Collections.Array<Vector2[]> result = Geometry2D.MergePolygons(mergedPolygon, polygons[i]);
            if(result.Count > 0){
                mergedPolygon = result[0];
            }else{
                GD.PrintErr($"Failed to merge polygons at index {i}");
                // Handle the case where polygons could not be merged.
            }
        }

        return mergedPolygon;
    }

    public static Vector2 GetRandomRespawn(){
        return respawnPoints[Game.Random.Next(0,respawnPoints.Count)].GlobalPosition;
    }

    public static Vector2 GetRandomRespawn(string team){
        List<Vector2> teamSpawns = new List<Vector2>();
        foreach(Node2D spawnPoint in respawnPoints){
            if(((string)spawnPoint.GetMeta("team")).Equals(team)) teamSpawns.Add(spawnPoint.GlobalPosition);
        }
        return teamSpawns[Game.Random.Next(0,teamSpawns.Count)];
    }

    public void HostSpawnPlayers(){
        if(Game.TotalPlayers != 1){
            for(int i = 0; i < Game.MAX_PLAYERS; i++){
                Node2D spawnToDelete = spawnPoints[Game.Random.Next(0,spawnPoints.Count)];
                spawnPoints.Remove(spawnToDelete);
                spawnToDelete.QueueFree();
            }
        }

        //Spawn Players
        if(TeamSportsMode.IsTeamMode() && (Online.IsHost() || !Online.PeerIsActive())) TeamSportsMode.SetTeams(); //Make host set teams so they sync
        //Multiplayer Random SpawnPoint
        if(Online.IsHost() || !Online.PeerIsActive()){
            if(Game.TotalPlayers != 1){
                Vector2[] playerSpawns = new Vector2[Game.TotalPlayers];
                bool[] flippedStarts = new bool[Game.TotalPlayers];
                //Race and Golf all players spawn at same point
                if(Game.CurrentMode == Mode.GameMode.Race || Game.CurrentMode == Mode.GameMode.Golf){
                    spawnPoints = new List<Node2D>();
                    foreach(Node levelNode in GetChildren()){
                        if(levelNode.IsInGroup("Spawn")){
			    	    	spawnPoints.Add(levelNode as Node2D);
			    	    }
                    }
                    Node2D theSpawner = spawnPoints[Game.Random.Next(0,spawnPoints.Count)];
                    for(int i = 0; i < Game.TotalPlayers; i++){
                        flippedStarts[i] = (theSpawner as Sprite2D).FlipH;
                        playerSpawns[i] = theSpawner.GlobalPosition;
                    }
                    theSpawner.QueueFree();
                //All other modes players spawn at unique spawn points
                }else{
                    for(int i = 0; i < Game.TotalPlayers; i++){
                        spawnPoints = new List<Node2D>();
			            foreach(Node levelNode in GetChildren()){
			    	        if(levelNode.IsInGroup("Spawn")){
			    	    	    if(!TeamSportsMode.IsTeamMode()){
			    	    		    Node2D spawner = levelNode as Node2D;
			    	    		    spawnPoints.Add(spawner);
			    	    	    }else if(((string)levelNode.GetMeta("team")).Equals(TeamSportsMode.Teams[i])){
			    	    		    Node2D spawner = levelNode as Node2D;
                                    spawnPoints.Add(spawner);
			    	    	    }
			    	        }
			            }
                        Node2D theSpawner = spawnPoints[Game.Random.Next(0,spawnPoints.Count)];
                        flippedStarts[i] = (theSpawner as Sprite2D).FlipH;
		                spawnPoints.Remove(theSpawner);
                        playerSpawns[i] = theSpawner.GlobalPosition;
                        theSpawner.Free();    
                    }
                }
                
                BitArray flipped = new BitArray(flippedStarts);
                byte[] flippedByte = new byte[1];
                flipped.CopyTo(flippedByte,0);
                if(!TeamSportsMode.IsTeamMode()) Rpc(nameof(SpawnPlayers),playerSpawns,flippedByte[0]);
                else Rpc(nameof(SpawnPlayers),playerSpawns,flippedByte[0],TeamSportsMode.Teams);
            //Solo consistent SpawnPoint
		    }else{
                spawnPoints = new List<Node2D>();
		    	foreach(Node levelNode in GetChildren()){
		    		if(levelNode.IsInGroup("Spawn")){
		    			if(!TeamSportsMode.IsTeamMode()){
		    				Node2D spawner = levelNode as Node2D;
		    				spawnPoints.Add(spawner);
		    			}else if(((string)levelNode.GetMeta("team")).Equals(TeamSportsMode.Teams[0])){
		    				Node2D spawner = levelNode as Node2D;
                            spawnPoints.Add(spawner);
		    			}
		    		}
		    	}
                Vector2[] playerSpawns = {spawnPoints[0].GlobalPosition};
                bool[] flippedStarts = {(spawnPoints[0] as Sprite2D).FlipH};
                BitArray flipped = new BitArray(flippedStarts);
                byte[] flippedByte = new byte[1];
                flipped.CopyTo(flippedByte,0);
                if(!TeamSportsMode.IsTeamMode()) Rpc(nameof(SpawnPlayers),playerSpawns,flippedByte[0]);
                else Rpc(nameof(SpawnPlayers),playerSpawns,flippedByte[0],TeamSportsMode.Teams);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnPlayers(Vector2[] playerSpawns,byte flippedStart,string[] teams){
        Game.Players = new Player[Game.TotalPlayers];
        for(int i = 0; i < Game.TotalPlayers; i++){
            Player player = GD.Load<PackedScene>("res://Source/Scenes/Players/Player.tscn").Instantiate<Player>();
            player.Id = (byte)(i+1);
            player.Name = "Player" + player.Id;
            Game.Players[i] = player;
            player.Visible = false;
            player.FlippedStart = (flippedStart & (1 << i)) != 0;
            player.SpawnPoint = playerSpawns[i];
            player.Team = teams[i];
			GetParent().AddChild(player);
        }
        if(TeamSportsMode.IsTeamMode()){
            TeamSportsMode.Teams = teams;
        }
        if(Mode.ModeNode is ILevelLoadedEvent levelLoad) levelLoad.OnLevelLoaded();
        //Mode.ModeNode.OnLevelLoaded();
        OnlineReadier onlineReadier = GetParent().GetNode<OnlineReadier>("OnlineReadier");
        if(Online.IsOnline) onlineReadier.RpcId(1,nameof(onlineReadier.ClientSpawnedPlayers));
    }
    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnPlayers(Vector2[] playerSpawns,byte flippedStart){
        Game.Players = new Player[Game.TotalPlayers];
        for(int i = 0; i < Game.TotalPlayers; i++){
            Player player = GD.Load<PackedScene>("res://Source/Scenes/Players/Player.tscn").Instantiate<Player>();
            player.Id = (byte)(i+1);
            player.Name = "Player" + player.Id;
            Game.Players[i] = player;
            player.Visible = false;
            player.FlippedStart = (flippedStart & (1 << i)) != 0;
            player.SpawnPoint = playerSpawns[i];
            GD.Print(player.Finished);
			GetParent().AddChild(player);
        }
        if(Mode.ModeNode is ILevelLoadedEvent levelLoad) levelLoad.OnLevelLoaded();
        //Mode.ModeNode.OnLevelLoaded();
        OnlineReadier onlineReadier = GetParent().GetNode<OnlineReadier>("OnlineReadier");
        if(Online.IsOnline) onlineReadier.RpcId(1,nameof(onlineReadier.ClientSpawnedPlayers));
    }

    public static Vector2 GetEdgePosition(Vector2 position, float angleInRadians, float width, float height){
        // Normalize the angle to the range [0, 2π]
        float angle = angleInRadians % (2 * MathF.PI);
        if (angle < 0) angle += 2 * MathF.PI;

        // Define the half extents
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Points for the rectangle's edges
        float arcTanAngle = MathF.Atan2(height, width);
        if(angle <= arcTanAngle || angle >= 2 * MathF.PI - arcTanAngle) // Right edge
            return new Vector2(halfWidth, Mathf.Clamp(position.Y, -halfHeight, halfHeight));
        if(angle <= MathF.PI - arcTanAngle) // Top edge
            return new Vector2(Mathf.Clamp(position.X, -halfWidth, halfWidth), halfHeight);
        if(angle <= MathF.PI + arcTanAngle) // Left edge
            return new Vector2(-halfWidth, Mathf.Clamp(position.Y, -halfHeight, halfHeight));
        // Bottom edge
            return new Vector2(Mathf.Clamp(position.X, -halfWidth, halfWidth), -halfHeight);
    }

    public static int DetermineEdge(Vector2 position, float width, float height){
        // Define the half extents
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Check which edge the position lies on
        if(position.X == halfWidth) return 0; //Right
        if(position.X == -halfWidth) return 1; //Left
        if(position.Y == halfHeight) return 2; //Bottom
        if(position.Y == -halfHeight) return 3; //Top
        return 4; // This should not happen with the given function
    }

    //To Check if a position is a reasonable distance offscreen but not teleported offscreen through something like a player dieing
    public static bool IsPositionOffscreen(Vector2 position){
        return position.Y>2500/LevelNode.CameraZoom || (position.Y<-2500/LevelNode.CameraZoom && position.Y>-10000/LevelNode.CameraZoom) || (position.X<-4444/LevelNode.CameraZoom && position.X>-17777/LevelNode.CameraZoom) || (position.X>4444/LevelNode.CameraZoom && position.X<17777/LevelNode.CameraZoom);
    }
    //Checks if a position is a reasonable distance offscreen
    public static bool IsPositionOffscreenOrDead(Vector2 position){
        return position.Y>2500/LevelNode.CameraZoom || position.Y<-2500/LevelNode.CameraZoom || position.X<-4444/LevelNode.CameraZoom || position.X>4444/LevelNode.CameraZoom;
    }
}

public struct Palette{
    public Color FloorColor;
    public Color InsideColor;
    public Color OutlineColor;
    public Palette(Color floorColor, Color insideColor, Color outlineColor){
        FloorColor = floorColor;
        InsideColor = insideColor;
        OutlineColor = outlineColor;
    }
}