using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

[Tool]
public partial class Level : Node2D {
	[Export]
	public int LevelUnit; //Laps for Race or Par for Golf (Par should be set based on an itemless hole)
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
	private List<Node2D> respawnPoints;
	private List<Node2D> spawnPoints;
	public const float OUTLINE_WIDTH = 9;
	private const float BAKE_INTERVAL = 50;
	private static readonly StringName META_INVERT = new StringName("invert");
	private static readonly StringName GROUP_BAKED_GEOMETRY = new StringName("BakedLevelGeometry");
	private static readonly StringName GROUP_SPAWN = new StringName("Spawn");
	private static readonly StringName GROUP_RESPAWN = new StringName("Respawn");
	private static readonly StringName GROUP_REGAIN = new StringName("Regain");
	private static readonly StringName GROUP_NO_REGAIN = new StringName("NoRegain");
	private static readonly StringName META_TEAM = new StringName("team");
	public static Level LevelNode;

	[ExportGroup("Level Generation")]
	[Export]
	public bool BakeLevelGeometry {
		get => false;
		set {
			if (value) {
				BakeLevel();
			}
		}
	}

	public override void _Ready(){
		if(Engine.IsEditorHint()) return;

		//Delete the editor Bodies at runtime so only the baked version exists in game
		foreach(Node child in GetChildren()){
			if(child is StaticBody2D staticBody && !staticBody.IsInGroup(GROUP_BAKED_GEOMETRY)){
				staticBody.QueueFree();
			}
		}

		LevelNode = this;
		Game.DisableProcesses(this);
		SetupCamera();
		CheckForColorOverrides();
		SetupBackground();

		GatherAndHideSpawnpoints();
		GetTree().Paused = true;
		//Make host set player spawnpoints
		if(Online.IsOnline){ 
			Game.Players = Array.Empty<Player>();
			Mode.ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Players/PlayerSynchronizer.tscn").Instantiate());
		}
	}

	private void BakeLevel(){
		if(!Engine.IsEditorHint()) return;
	
		GD.Print("Cleaning up old bake...");
		ClearPreviousBake();
	
		GD.Print("Baking Level Geometry...");
		List<StaticBody2D> editorBodies = GatherEditorBodies();
		List<StaticBody2D> bakedBodies = CreateBakedBodies(editorBodies);
		
		List<CollisionPolygon2D>[] collisions = ConvertPathsToPolygons(editorBodies, bakedBodies);
		ExtractAndCenterCollisions(editorBodies, bakedBodies, collisions);
		
		// YOUR RESTORED LOGIC
		MergeInvertedPolygons(collisions);
		
		GenerateLevelVisuals(collisions, bakedBodies);

		foreach (StaticBody2D body in editorBodies) {
			body.Visible = false; 
		}

		GD.Print("Bake Complete! Save the scene (Ctrl+S).");
	}

	private void ClearPreviousBake(){
		Godot.Collections.Array<Node> oldNodes = GetTree().GetNodesInGroup(GROUP_BAKED_GEOMETRY);
		foreach(Node node in oldNodes){
			if(IsInstanceValid(node) && IsAncestorOf(node)){
				node.Free();
			}
		}
	}

	private List<StaticBody2D> GatherEditorBodies(){
		List<StaticBody2D> editorBodies = new List<StaticBody2D>();
		foreach(Node child in GetChildren()){
			if(child is StaticBody2D staticBody && !staticBody.IsInGroup(GROUP_BAKED_GEOMETRY)){
				editorBodies.Add(staticBody);
			}
		}
		return editorBodies;
	}

	private List<StaticBody2D> CreateBakedBodies(List<StaticBody2D> editorBodies){
		List<StaticBody2D> bakedBodies = new List<StaticBody2D>();
		foreach(StaticBody2D editorBody in editorBodies){
			StaticBody2D bakedBody = new StaticBody2D();
			bakedBody.Position = editorBody.Position;
			bakedBody.Name = editorBody.Name + "_Baked";
			bakedBody.CollisionLayer = 0b11;

			if(editorBody.IsInGroup(GROUP_NO_REGAIN)){
				bakedBody.AddToGroup(GROUP_NO_REGAIN, true);
				bakedBody.Modulate = new Color(0.85f,0.85f,0.85f);
			}else if(!editorBody.IsInGroup(GROUP_REGAIN)){
				GD.PrintErr($"{Game.CurrentLevelName}: FORGOT TO ASSIGN EITHER Regain or NoRegain group to {editorBody.Name} Node");
			}else{
				bakedBody.AddToGroup(GROUP_REGAIN, true);
			}

			bakedBody.AddToGroup(GROUP_BAKED_GEOMETRY, true);
			
			AddChild(bakedBody);
			if(Engine.IsEditorHint()) bakedBody.Owner = GetTree().EditedSceneRoot;
			
			bakedBodies.Add(bakedBody);
		}
		return bakedBodies;
	}

	private List<CollisionPolygon2D>[] ConvertPathsToPolygons(List<StaticBody2D> editorBodies, List<StaticBody2D> bakedBodies){
		List<CollisionPolygon2D>[] collisions = new List<CollisionPolygon2D>[editorBodies.Count];
		for(int i = 0; i < editorBodies.Count; i++){
			collisions[i] = new List<CollisionPolygon2D>();
			StaticBody2D editorBody = editorBodies[i];
			StaticBody2D bakedBody = bakedBodies[i];

			foreach(Node child in editorBody.GetChildren()){
				if(child is Path2D path){
					if(path.Curve.BakeInterval != BAKE_INTERVAL) path.Curve.BakeInterval = BAKE_INTERVAL;
					CollisionPolygon2D pathCollisionPolygon = new CollisionPolygon2D();
					Vector2[] polygon = path.Curve.GetBakedPoints();
					for(int j = 0; j < polygon.Length; j++){
						polygon[j] = path.Position + (path.Rotation == 0 ? polygon[j] : polygon[j].Rotated(path.Rotation));
					}
					pathCollisionPolygon.Name = path.Name + "Polygon";
					pathCollisionPolygon.Polygon = polygon;
					pathCollisionPolygon.ZIndex = path.ZIndex;
					if(path.HasMeta(META_INVERT)){
						pathCollisionPolygon.SetMeta(META_INVERT,(bool)path.GetMeta(META_INVERT));
					}
					
					bakedBody.AddChild(pathCollisionPolygon);
					if(Engine.IsEditorHint()) pathCollisionPolygon.Owner = GetTree().EditedSceneRoot;

					collisions[i].Add(pathCollisionPolygon);
				}
			}
		}
		return collisions;
	}

	private void ExtractAndCenterCollisions(List<StaticBody2D> editorBodies, List<StaticBody2D> bakedBodies, List<CollisionPolygon2D>[] collisions){
		for(int i = 0; i < editorBodies.Count; i++){
			StaticBody2D editorBody = editorBodies[i];
			StaticBody2D bakedBody = bakedBodies[i];

			foreach(Node child in editorBody.GetChildren()){
				if(child is CollisionPolygon2D manualPolygon){
					CollisionPolygon2D dupe = (CollisionPolygon2D)manualPolygon.Duplicate(0);
					bakedBody.AddChild(dupe);
					if(Engine.IsEditorHint()) dupe.Owner = GetTree().EditedSceneRoot;
					collisions[i].Add(dupe);
				}
			}

			foreach(CollisionPolygon2D collisionPolygon in collisions[i]){
				if(collisionPolygon.Position != Vector2.Zero){
					Vector2[] newPolygon = (Vector2[])collisionPolygon.Polygon.Clone();
					for(int j = 0; j < collisionPolygon.Polygon.Length; j++){
						newPolygon[j] = collisionPolygon.Polygon[j] + collisionPolygon.Position;
					}
					collisionPolygon.Position = Vector2.Zero;
					collisionPolygon.Polygon = newPolygon;
				}
				if(collisionPolygon.HasMeta("inverted") || collisionPolygon.HasMeta("Inverted") || collisionPolygon.HasMeta("Invert"))
					GD.PrintErr("WRONG META NAME ASSIGNED TO " + collisionPolygon.Name);
			}
		}
	}

	// EXACT ORIGINAL LOGIC RESTORED
	private void MergeInvertedPolygons(List<CollisionPolygon2D>[] collisions){
		for(int i = 0; i < collisions.Length; i++){
			List<CollisionPolygon2D> collisionPolygons = collisions[i];
			for(int j = 0; j < collisionPolygons.Count; j++){
				for(int k = collisionPolygons.Count - 1; k > j; k--){
					if(collisionPolygons[j].HasMeta(META_INVERT) == collisionPolygons[k].HasMeta(META_INVERT)){
						Vector2 duplicatePointJ = Vector2.Inf;
						Vector2 movedDupePointJ = Vector2.Inf;
						
						Vector2 duplicatePointK = Vector2.Inf;
						Vector2 movedDupePointK = Vector2.Inf;

						CollisionPolygon2D collisionJ = collisionPolygons[j];
						CollisionPolygon2D collisionK = collisionPolygons[k];

						if(collisionJ.HasMeta(META_INVERT) && (bool)collisionJ.GetMeta(META_INVERT)){
							// --- 1. Offset Polygon J's Seam ---
							duplicatePointJ = findDuplicatePoint(collisionJ.Polygon);
							if(duplicatePointJ != Vector2.Inf){
								Vector2[] points = (Vector2[])collisionJ.Polygon.Clone();
								int dupeIndex = Array.IndexOf(points, duplicatePointJ); 
								for(int l = 0; l < 4; l++){
									switch(l){
										case 0: movedDupePointJ = duplicatePointJ + Vector2.Right; break;
										case 1: movedDupePointJ = duplicatePointJ + Vector2.Left; break;
										case 2: movedDupePointJ = duplicatePointJ + Vector2.Up; break;
										case 3: movedDupePointJ = duplicatePointJ + Vector2.Down; break;
									}
									points[dupeIndex] = movedDupePointJ;
									if(Geometry2D.DecomposePolygonInConvex(points).Count != 0) break;
									else GD.Print("Ignore the Convex Error above"); 
								}
								collisionJ.Polygon = points;
							}

							// --- 2. Offset Polygon K's Seam ---
							duplicatePointK = findDuplicatePoint(collisionK.Polygon);
							if(duplicatePointK != Vector2.Inf){
								Vector2[] points = (Vector2[])collisionK.Polygon.Clone();
								int dupeIndex = Array.IndexOf(points, duplicatePointK); 
								for(int l = 0; l < 4; l++){
									switch(l){
										case 0: movedDupePointK = duplicatePointK + Vector2.Right; break;
										case 1: movedDupePointK = duplicatePointK + Vector2.Left; break;
										case 2: movedDupePointK = duplicatePointK + Vector2.Up; break;
										case 3: movedDupePointK = duplicatePointK + Vector2.Down; break;
									}
									points[dupeIndex] = movedDupePointK;
									if(Geometry2D.DecomposePolygonInConvex(points).Count != 0) break;
									else GD.Print("Ignore the Convex Error above"); 
								}
								collisionK.Polygon = points;
							}
						}

						// --- 3. Attempt the Merge ---
						Godot.Collections.Array<Vector2[]> mergedPolygons = Geometry2D.MergePolygons(collisionJ.Polygon, collisionK.Polygon);
						
						if(mergedPolygons.Count == 1){
							Vector2[] mergedPoints = mergedPolygons[0];

							if(collisionJ.HasMeta(META_INVERT) && (bool)collisionJ.GetMeta(META_INVERT)){
								// --- 4. Restore BOTH J and K's points inside the newly merged shape! ---
								if(duplicatePointJ != Vector2.Inf){
									int indexJ = Array.IndexOf(mergedPoints, movedDupePointJ);
									if(indexJ != -1) mergedPoints[indexJ] = duplicatePointJ;
								}
								if(duplicatePointK != Vector2.Inf){
									int indexK = Array.IndexOf(mergedPoints, movedDupePointK);
									if(indexK != -1) mergedPoints[indexK] = duplicatePointK;
								}
								collisionJ.SetMeta(META_INVERT, true);
							}
							
							collisionJ.Polygon = mergedPoints;
							collisionPolygons[k].Free();
							collisionPolygons.RemoveAt(k);
						} else {
							// If they fail to merge, safely restore both points to their original polygons so visuals aren't ruined
							if(collisionJ.HasMeta(META_INVERT) && (bool)collisionJ.GetMeta(META_INVERT)){
								if(duplicatePointJ != Vector2.Inf){
									Vector2[] pointsJ = (Vector2[])collisionJ.Polygon.Clone();
									int indexJ = Array.IndexOf(pointsJ, movedDupePointJ);
									if(indexJ != -1) pointsJ[indexJ] = duplicatePointJ;
									collisionJ.Polygon = pointsJ;
								}
								if(duplicatePointK != Vector2.Inf){
									Vector2[] pointsK = (Vector2[])collisionK.Polygon.Clone();
									int indexK = Array.IndexOf(pointsK, movedDupePointK);
									if(indexK != -1) pointsK[indexK] = duplicatePointK;
									collisionK.Polygon = pointsK;
								}
							}
						}		
						
						Vector2 findDuplicatePoint(Vector2[] polygon){
							for(int index = 0; index < polygon.Length; index++){
								for(int jndex = index+1; jndex < polygon.Length; jndex++){
									if(polygon[index].IsEqualApprox(polygon[jndex])) return polygon[index];
								}
							}
							return Vector2.Inf;
						}
					}
				}
			}
		}
	}

	private void GenerateLevelVisuals(List<CollisionPolygon2D>[] collisions, List<StaticBody2D> bakedBodies){
		GDScript aaPolygonScript = GD.Load<GDScript>("res://addons/antialiased_line2d/antialiased_polygon2d.gd");
		
		Palette defaultPalette = GetDefaultModePalette();
		Color bakeFloor = floorColorOverride.Equals(Game.ZEROES) ? defaultPalette.FloorColor : floorColorOverride;
		Color bakeInside = InsideColorOverride.Equals(Game.ZEROES) ? defaultPalette.InsideColor : InsideColorOverride;
		Color bakeOutline = OutlineColorOverride.Equals(Game.ZEROES) ? defaultPalette.OutlineColor : OutlineColorOverride;
		
		Texture2D bakeTexture = groundTexture;
		if(bakeTexture == null){
			bakeTexture = GetDefaultGroundTexture();
		}

		for(int i = 0; i < collisions.Length; i++){
			List<CollisionPolygon2D> collisionPolygons = collisions[i];
			foreach(CollisionPolygon2D collisionPolygon in collisionPolygons){
				bool invert = false;
				Vector2[] visualPolygon;
				float maxOuterDistance = 128;
				if(collisionPolygon.HasMeta(META_INVERT)){ 
					invert = (bool)collisionPolygon.GetMeta(META_INVERT);
					Vector2[] clipPolygon = new Vector2[] {
						Vector2.Inf, 
						new Vector2(float.NegativeInfinity,float.PositiveInfinity), 
						new Vector2(float.NegativeInfinity,float.NegativeInfinity), 
						new Vector2(float.PositiveInfinity,float.NegativeInfinity)  
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
				
				Polygon2D topPolygon = new Polygon2D();
				topPolygon.Color = bakeFloor;
				Polygon2D insidePolygon = new Polygon2D();
				insidePolygon.Color = bakeInside;
				insidePolygon.Texture = bakeTexture;
				insidePolygon.TextureRepeat = TextureRepeatEnum.Enabled;
				
				insidePolygon.Polygon = visualPolygon;
				Vector2[] topPolygonArr = visualPolygon;
				Vector2[] topCollisionArr;
				for(int j = 0; j < topPolygonArr.Length; j++){
					topPolygonArr[j] += new Vector2(0,-32f);
				}
				topPolygon.ZIndex = insidePolygon.ZIndex - 1;

				Dictionary<Vector2,Tuple<bool,bool>> newPoints = new Dictionary<Vector2, Tuple<bool,bool>>();
				for(int j = 0; j < topPolygonArr.Length; j++){
					Vector2 point = topPolygonArr[j];
					Vector2 previousPoint = j != 0 ? topPolygonArr[j-1] : topPolygonArr[topPolygonArr.Length-1];
					Vector2 nextPoint = j != topPolygonArr.Length-1 ? topPolygonArr[j+1] : topPolygonArr[0];
					bool hasPointBelow = true;
					bool isNext = false;
					if(point.Y < previousPoint.Y && MathF.Abs(point.X - previousPoint.X) > 0.01f){
						if(point.X > previousPoint.X && point.X > nextPoint.X){
							hasPointBelow = false;
							isNext = false;
						}else if(point.X < previousPoint.X && point.X < nextPoint.X){
							hasPointBelow = false;
							isNext = false;
						} 
					}else if(point.Y < nextPoint.Y && MathF.Abs(point.X - nextPoint.X) > 0.01f){
						if(point.X > nextPoint.X && point.X > previousPoint.X){ 
							hasPointBelow = false;
							isNext = true;
						}else if(point.X < nextPoint.X && point.X < previousPoint.X){
							hasPointBelow = false;
							isNext = true;
						}
					}
					if(!hasPointBelow){
						newPoints.Add(topPolygonArr[j],new Tuple<bool, bool>(isNext, false));
					}
					bool hasPointAbove = true;
					if(point.Y > previousPoint.Y && MathF.Abs(point.X - previousPoint.X) > 0.01f){
						if(point.X > previousPoint.X && point.X > nextPoint.X){
							hasPointAbove = false;
							isNext = false;
						}else if(point.X < previousPoint.X && point.X < nextPoint.X){
							hasPointAbove = false;
							isNext = false;
						}
					}else if(point.Y > nextPoint.Y && MathF.Abs(point.X - nextPoint.X) > 0.01f){
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
						}catch{ }
					}
				}
				List<Vector2> polygonPoints = new List<Vector2>();
				for(int j = 0; j < topPolygonArr.Length; j++){
					polygonPoints.Add(topPolygonArr[j]);
				}
				foreach(Vector2 point in newPoints.Keys){
					if(!newPoints[point].Item2){ 
						polygonPoints.Insert(polygonPoints.IndexOf(point) + (newPoints[point].Item1 ? 1 : 0),point + new Vector2(0,32f));
					}else{ 
						polygonPoints.Insert(polygonPoints.IndexOf(point) + (newPoints[point].Item1 ? 0 : 1),point + new Vector2(0,32f));
					}
				}
				
				topPolygonArr = polygonPoints.ToArray();
				topPolygon.Polygon = topPolygonArr;

				topCollisionArr = (Vector2[])collisionPolygon.Polygon.Clone();
				for(int j = 0; j < topCollisionArr.Length; j++){
					topCollisionArr[j] -= new Vector2(0,17f);
				}

				if(!invert){
					Godot.Collections.Array<Vector2[]> mergedPhysics = Geometry2D.MergePolygons(collisionPolygon.Polygon, topCollisionArr);
					if (mergedPhysics.Count > 0) {
						collisionPolygon.Polygon = mergedPhysics[0]; 
						for (int m = 1; m < mergedPhysics.Count; m++) {
							CollisionPolygon2D extraPoly = new CollisionPolygon2D();
							extraPoly.Position = collisionPolygon.Position;
							extraPoly.Polygon = mergedPhysics[m];
							bakedBodies[i].AddChild(extraPoly);
							if (Engine.IsEditorHint()) extraPoly.Owner = GetTree().EditedSceneRoot;
							bakedBodies[i].MoveChild(extraPoly, 0);
						}
					}
				}else{
					CollisionPolygon2D topCollision = new CollisionPolygon2D();
					topCollision.Position = collisionPolygon.Position;
					topCollision.Polygon = topCollisionArr;
					bakedBodies[i].AddChild(topCollision);
					if (Engine.IsEditorHint()) topCollision.Owner = GetTree().EditedSceneRoot;
					bakedBodies[i].MoveChild(topCollision, 0);
				}
				bakedBodies[i].MoveChild(collisionPolygon, -1);
				
				GodotObject aaInsidePolygon = (GodotObject)aaPolygonScript.New();
				Vector2[] insideArr = insidePolygon.Polygon;
				aaInsidePolygon.Call("set_polygon",insideArr);
				aaInsidePolygon.Set("texture",bakeTexture);
				aaInsidePolygon.Set("texture_repeat",(int)TextureRepeatEnum.Enabled);
				aaInsidePolygon.Call("set_stroke_width",10);
				aaInsidePolygon.Call("set_stroke_color",bakeOutline);
				aaInsidePolygon.Set("color",new Color(bakeInside.R,bakeInside.G,bakeInside.B,bakeInside.A));
				aaInsidePolygon.Set("z_index",insidePolygon.ZIndex);
				if(invert){
					(aaInsidePolygon as Polygon2D).InvertEnabled = true;
					(aaInsidePolygon as Polygon2D).InvertBorder = maxOuterDistance;
				}
				collisionPolygon.AddChild(aaInsidePolygon as Node);
				if (Engine.IsEditorHint()) (aaInsidePolygon as Node).Owner = GetTree().EditedSceneRoot;
				insidePolygon.Free();

				GodotObject aaTopPolygon = (GodotObject)aaPolygonScript.New();
				Vector2[] topArr = topPolygon.Polygon;
				aaTopPolygon.Call("set_polygon",topArr);
				aaTopPolygon.Call("set_stroke_width",10);
				aaTopPolygon.Call("set_stroke_color",bakeOutline);
				aaTopPolygon.Set("color",new Color(bakeFloor.R,bakeFloor.G,bakeFloor.B,bakeFloor.A));
				aaTopPolygon.Set("z_index",topPolygon.ZIndex-1);
				(aaTopPolygon as Polygon2D).LightMask = 0b11;
				CallDeferred(nameof(SetDeferredLightmaskForLine), aaTopPolygon as Polygon2D);
				if(invert){
					(aaTopPolygon as Polygon2D).InvertEnabled = true;
					(aaTopPolygon as Polygon2D).InvertBorder = maxOuterDistance;
				}
				collisionPolygon.AddChild(aaTopPolygon as Node);
				if (Engine.IsEditorHint()) (aaTopPolygon as Node).Owner = GetTree().EditedSceneRoot;
				topPolygon.Free();
			}
		}
	}

	private void SetupCamera(){
		Game.UpdateContentScaleVector();
		Game.Camera.Zoom = Game.ContentScaleVector2 * CameraZoom;
		CanvasLayer backgroundLayer = Game.GameNode.GetNode<CanvasLayer>("BackgroundLayer");
		backgroundLayer.Scale = Game.ContentScaleVector2;
	}

	private void CheckForColorOverrides(){
		Palette defaultPalette = GetDefaultModePalette();
		if(floorColorOverride.Equals(Game.ZEROES)) floorColorOverride = defaultPalette.FloorColor;
		if(InsideColorOverride.Equals(Game.ZEROES)) InsideColorOverride = defaultPalette.InsideColor;
		if(OutlineColorOverride.Equals(Game.ZEROES)) OutlineColorOverride = defaultPalette.OutlineColor;
	}

	private void SetupBackground(){
		if(groundTexture == null){
			groundTexture = GetDefaultGroundTexture();
		} 
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
	}

	private void GatherAndHideSpawnpoints(){
		spawnPoints = new List<Node2D>();
		respawnPoints = new List<Node2D>();
		foreach(Node node in GetChildren()){
			if(node.IsInGroup(GROUP_SPAWN)){
				Node2D spawnpoint = node as Node2D;
				spawnPoints.Add(spawnpoint);
				spawnpoint.Visible = false;
			}else if(node.IsInGroup(GROUP_RESPAWN)){
				Sprite2D respawnPoint = node as Sprite2D;
				respawnPoint.Visible = false;
				respawnPoints.Add(node as Node2D);
			}
		}
	}

	private void SetDeferredLightmaskForLine(Polygon2D polygon){
		polygon.GetChild<Node2D>(0).LightMask = 0b11;
	}

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
			}
		}

		return mergedPolygon;
	}

	public static Vector2 GetRandomRespawn(){
		return LevelNode.respawnPoints[Game.Random.Next(0,LevelNode.respawnPoints.Count)].GlobalPosition;
	}

	public static Vector2 GetRandomRespawn(string team){
		List<Vector2> teamSpawns = new List<Vector2>();
		foreach(Node2D spawnPoint in LevelNode.respawnPoints){
			if(((string)spawnPoint.GetMeta(META_TEAM)).Equals(team)) teamSpawns.Add(spawnPoint.GlobalPosition);
		}
		return teamSpawns[Game.Random.Next(0,teamSpawns.Count)];
	}

	public void HostSpawnPlayers(){
		if(Game.TotalPlayers != 1){
			for(int i = 0; i < Game.MAX_PLAYERS; i++){
				if(spawnPoints.Count == 0) break;
				Node2D spawnToDelete = spawnPoints[Game.Random.Next(0,spawnPoints.Count)];
				spawnPoints.Remove(spawnToDelete);
				spawnToDelete.QueueFree();
			}
		}

		if(TeamSportsMode.IsTeamMode() && (Online.IsHost() || !Online.PeerIsActive())) TeamSportsMode.SetTeams(); 
		if(Online.IsHost() || !Online.PeerIsActive()){
			if(Game.TotalPlayers != 1){
				Vector2[] playerSpawns = new Vector2[Game.TotalPlayers];
				bool[] flippedStarts = new bool[Game.TotalPlayers];
				if(Game.CurrentMode == Mode.GameMode.Race || Game.CurrentMode == Mode.GameMode.Golf){
					Node2D theSpawner = spawnPoints[Game.Random.Next(0,spawnPoints.Count)];
					for(int i = 0; i < Game.TotalPlayers; i++){
						flippedStarts[i] = (theSpawner as Sprite2D).FlipH;
						playerSpawns[i] = theSpawner.GlobalPosition;
					}
					theSpawner.QueueFree();
				}else{
					for(int i = 0; i < Game.TotalPlayers; i++){
						List<Node2D> validSpawns = new List<Node2D>();
						foreach(Node2D spawner in spawnPoints){
							if(!TeamSportsMode.IsTeamMode()){
								validSpawns.Add(spawner);
							}else if(((string)spawner.GetMeta(META_TEAM)).Equals(TeamSportsMode.Teams[i])){
								validSpawns.Add(spawner);
							}
						}
						Node2D theSpawner = validSpawns[Game.Random.Next(0,validSpawns.Count)];
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
			}else{
				List<Node2D> validSpawns = new List<Node2D>();
				foreach(Node2D spawner in spawnPoints){
					if(!TeamSportsMode.IsTeamMode()){
						validSpawns.Add(spawner);
					}else if(((string)spawner.GetMeta(META_TEAM)).Equals(TeamSportsMode.Teams[0])){
						validSpawns.Add(spawner);
					}
				}
				Vector2[] playerSpawns = {validSpawns[0].GlobalPosition};
				bool[] flippedStarts = {(validSpawns[0] as Sprite2D).FlipH};
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
		PackedScene playerScene = GD.Load<PackedScene>("res://Source/Scenes/Players/Player.tscn");
		for(int i = 0; i < Game.TotalPlayers; i++){
			Player player = playerScene.Instantiate<Player>();
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
		OnlineReadier onlineReadier = GetParent().GetNode<OnlineReadier>("OnlineReadier");
		if(Online.IsOnline) onlineReadier.RpcId(1,nameof(onlineReadier.ClientSpawnedPlayers));
	}
	
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SpawnPlayers(Vector2[] playerSpawns,byte flippedStart){
		Game.Players = new Player[Game.TotalPlayers];
		PackedScene playerScene = GD.Load<PackedScene>("res://Source/Scenes/Players/Player.tscn");
		for(int i = 0; i < Game.TotalPlayers; i++){
			Player player = playerScene.Instantiate<Player>();
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
		OnlineReadier onlineReadier = GetParent().GetNode<OnlineReadier>("OnlineReadier");
		if(Online.IsOnline) onlineReadier.RpcId(1,nameof(onlineReadier.ClientSpawnedPlayers));
	}

	public static Vector2 GetEdgePosition(Vector2 position, float angleInRadians, float width, float height){
		float angle = angleInRadians % (2 * MathF.PI);
		if (angle < 0) angle += 2 * MathF.PI;

		float halfWidth = width / 2f;
		float halfHeight = height / 2f;

		float arcTanAngle = MathF.Atan2(height, width);
		if(angle <= arcTanAngle || angle >= 2 * MathF.PI - arcTanAngle) 
			return new Vector2(halfWidth, Mathf.Clamp(position.Y, -halfHeight, halfHeight));
		if(angle <= MathF.PI - arcTanAngle) 
			return new Vector2(Mathf.Clamp(position.X, -halfWidth, halfWidth), halfHeight);
		if(angle <= MathF.PI + arcTanAngle) 
			return new Vector2(-halfWidth, Mathf.Clamp(position.Y, -halfHeight, halfHeight));
		return new Vector2(Mathf.Clamp(position.X, -halfWidth, halfWidth), -halfHeight);
	}

	public static int DetermineEdge(Vector2 position, float width, float height){
		float halfWidth = width / 2f;
		float halfHeight = height / 2f;

		if(position.X == halfWidth) return 0; 
		if(position.X == -halfWidth) return 1; 
		if(position.Y == halfHeight) return 2; 
		if(position.Y == -halfHeight) return 3; 
		return 4; 
	}

	public static bool IsPositionOffscreen(Vector2 position){
		return position.Y>2500/LevelNode.CameraZoom || (position.Y<-2500/LevelNode.CameraZoom && position.Y>-10000/LevelNode.CameraZoom) || (position.X<-4444/LevelNode.CameraZoom && position.X>-17777/LevelNode.CameraZoom) || (position.X>4444/LevelNode.CameraZoom && position.X<17777/LevelNode.CameraZoom);
	}
	
	public static bool IsPositionOffscreenOrDead(Vector2 position){
		return position.Y>2500/LevelNode.CameraZoom || position.Y<-2500/LevelNode.CameraZoom || position.X<-4444/LevelNode.CameraZoom || position.X>4444/LevelNode.CameraZoom;
	}

	private string GetSceneFolderName(){
		string path = this.SceneFilePath;
	
		if (string.IsNullOrEmpty(path)) {
			GD.PrintErr("Please save the scene first to get its folder name.");
			return "";
		}

		string[] pathParts = path.Split('/');
		string folderName = pathParts[pathParts.Length - 2];
		
		if (folderName.EndsWith(" Levels")) {
			folderName = folderName.Substring(0, folderName.Length - 7); 
		}
		
		return folderName;
	}

	private Mode.GameMode GetLevelMode(){
		if(Engine.IsEditorHint()){
			return Mode.StringToEnum(GetSceneFolderName());
		}else{
			return Game.CurrentMode;
		}
	}

	private Texture2D GetDefaultGroundTexture(){
		string modeStr = Mode.EnumToString(GetLevelMode());
		
		if(string.IsNullOrEmpty(modeStr) || modeStr == "Undefined Mode"){
			modeStr = "Miscellaneous"; 
		}
		
		string path = "res://Assets/Sprites/Level Stuff/Ground Patterns/" + modeStr + " Tile.png";
		
		if(ResourceLoader.Exists(path)){
			return GD.Load<Texture2D>(path);
		}else{
			GD.PrintErr("Could not find default ground texture at: " + path);
			return null;
		}
	}

	public Palette GetDefaultModePalette(){
		switch(GetLevelMode()){
			case Mode.GameMode.Race: return new Palette(new Color(1,173/255f,33/255f),new Color(1,128/255f,33/255f),new Color(255/255f,97/255f,0));
			case Mode.GameMode.Deathmatch: return new Palette(new Color(0,125/255f,1),new Color(0,27/255f,1),Colors.Black);
			case Mode.GameMode.Golf: return new Palette(new Color(0,1,201/255f),new Color(0,1,139/255f),new Color(0,189/255f,60/255f));
			case Mode.GameMode.KingOfTheHill: return new Palette(new Color(0,250/255f,0),new Color(0,195/255f,0),new Color(0,125/255f,0));
			case Mode.GameMode.CrownTheKing: return new Palette(new Color(1,238/255f,1),new Color(1,202/255f,1),new Color(210/255f,160/255f,210/255f));
			case Mode.GameMode.HotPotato: return new Palette(new Color(0.5f,0.5f,0.5f),new Color(0.25f,0.25f,0.25f),new Color(0.0625f,0.0625f,0.0625f));
			case Mode.GameMode.Domination: return new Palette(Color.Color8(240,240,240),Color.Color8(200,200,200),Colors.Black);
			case Mode.GameMode.BallinToTheBank: return new Palette(Color.Color8(255,230,0),Color.Color8(255,196,0),Color.Color8(255,255,0));
			case Mode.GameMode.TargetTest: return new Palette(Color.Color8(176,254,118),Color.Color8(129,233,121),Color.Color8(143,187,153));
			case Mode.GameMode.Soccer: return new Palette(new Color(0,145/255f,0),new Color(0,200/255f,0),new Color(0,64/255f,0));
			case Mode.GameMode.Volleyball: return new Palette(new Color(1,1,207/255f),new Color(1,1,149/255f),new Color(1,200/255f,0));
			case Mode.GameMode.BombBall: return new Palette(Color.Color8(207,186,225),Color.Color8(197,159,201),Color.Color8(133,0,249));
			case Mode.GameMode.Payload: return new Palette(Color.Color8(253,152,63),Color.Color8(214,81,8),Color.Color8(89,31,10));
			default: return new Palette(Game.ZEROES,Game.ZEROES,Game.ZEROES);
		}
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