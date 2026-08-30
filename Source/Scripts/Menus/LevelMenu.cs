using Godot;
using System;
using System.Collections.Generic;

public partial class LevelMenu : ScrollableMenu{
	private static List<string> optionNames;
	public static List<string> FoldersOpened;
	private string lastMenu;
	private Node selectionsNode;
	private const float X_POS = -1920;
	private const float SPACING = 250;
	private float yPos = -800;
	private Node2D levelVisual = null;
	private int inputId = (int)Game.PlayerDatas[0].InputDevice;
	private Polygon2D previewPoly;
	private GradientTexture2D bgGradient = null;
	private RichTextLabel soloStatsLabel;
	private string soloLevelDataText;

	public override void _Ready(){
		base._Ready();
		Tour.ResetPlayerScores();
		if(FoldersOpened == null) FoldersOpened = new List<string> {""};
		optionNames = new List<string>();
		if(Game.TotalPlayers > 1 || Online.IsOnline) lastMenu = "ModeMenu";
		else lastMenu = "SoloMenu";
		selectionsNode = GetNode<Node>("Selections");
		GetNode<Label>("Label").Text = Mode.EnumToString(Game.CurrentMode) + " Levels";
		int index = 0;
		foreach(string folder in DirAccess.GetDirectoriesAt(Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + string.Join("",FoldersOpened))){
			Label folderLabel = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn").Instantiate<Label>();
			optionNames.Add(folder + "/");
			folderLabel.Text = folder;
			folderLabel.Name = "Folder" + index;
			folderLabel.Position = new Vector2(X_POS,yPos);
			folderLabel.Scale = Vector2.One;
			yPos += SPACING;
			selectionsNode.AddChild(folderLabel);
			index++;
		}
		foreach(string file in DirAccess.GetFilesAt(Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + string.Join("",FoldersOpened))){
			Label levelLabel = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn").Instantiate<Label>();
			string newFile = file;
			if(file.Contains(".remap")) newFile = file.Replace(".remap","");
			optionNames.Add(newFile);
			levelLabel.Text = newFile.Replace(".tscn","");
			levelLabel.Name = "Level" + index;
			levelLabel.Position = new Vector2(X_POS,yPos);
			levelLabel.Scale = Vector2.One;
			yPos += SPACING;
			selectionsNode.AddChild(levelLabel);
			index++;
		}
		Selection = 1;
		totalSelections = optionNames.Count;
		previewPoly = GetNode<Polygon2D>("LevelPreview");
		string bgPath = $"res://Assets/Gradients/{Mode.EnumToString(Game.CurrentMode)}.tres";
		if(ResourceLoader.Exists(bgPath)){
			bgGradient = GD.Load<GradientTexture2D>(bgPath);
		}
		soloStatsLabel = GetNode<RichTextLabel>("SoloStats");
		if(Game.TotalPlayers > 1) soloStatsLabel.Visible = false;
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(!Game.UsingMouse() && inputId < (int)PlayerData.PlayerInputDevice.Mouse){
			InputChecks(delta,inputId);
			if(Input.IsActionJustReleased("Y" + inputId)){
				Selection = new Random().Next(1,optionNames.Count + 1);
				UpdateSelectionVisual();
			}	
		}else InputChecks(delta);
	}
	
	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		foreach(Node node in selectionsNode.GetChildren()){
			Label label;
			if(node is Label) label = node as Label;
			else break;
			
			if(label.Name.ToString().Equals("Level" + (choice - 1))){
				if(Game.TotalPlayers == 1){
					if(Game.CurrentMode == Mode.GameMode.Race) RaceHUD.LevelName = string.Join("",FoldersOpened) + optionNames[choice - 1];
					else if(Game.CurrentMode == Mode.GameMode.Survival) SurvivalHUD.LevelName = string.Join("",FoldersOpened) + optionNames[choice - 1];
				}
				Game.SetLevel(Game.CurrentMode,optionNames[choice - 1],string.Join("",FoldersOpened));
				
				if(!Online.IsOnline){
					MenuScene.MenuBackgroundFadeout();
					SceneTransitioner.SwitchToScene(Game.SceneType.Game);
				}else{
					if(OnlineLobby.Lobby != null) OnlineLobby.Lobby.StartGame();
				}
				
			}else if(label.Name.ToString().Equals("Folder" + (choice - 1)) && (!GolfCup.IsCup || !(optionNames[choice - 1].Contains("Cup")))){
				FolderNavigation(true);
			}else if(label.Name.ToString().Equals("Folder" + (choice - 1)) && GolfCup.IsCup && optionNames[choice - 1].Contains("Cup")){
				GolfCup.PrepareCup(FoldersOpened);
				MenuScene.MenuBackgroundFadeout();
				SceneTransitioner.SwitchToScene(Game.SceneType.Game);
			}
		}
	}
	
	public override void MenuBack(){
		SFX.Play("Back");
		if(FoldersOpened.Count <= 1){
			if(lastMenu == "ModeMenu" && Online.IsOnline){
				MenuScene.MenuNode.AddChild(GD.Load<PackedScene>(MenuScene.MENU_PATH + "ModeMenu" + ".tscn").Instantiate<Node>());
				QueueFree();
			}else{
				MenuScene.LoadMenu(lastMenu);
			}
		}else{
			FolderNavigation(false);
		}
	}

	protected override void UpdateSelectionVisual(){
		base.UpdateSelectionVisual();
		foreach(Node node in selectionsNode.GetChildren()){
			Label label = node as Label;
			if(label.Name.ToString().Contains("Level")){
				if(label.Name.Equals("Level" + (Selection - 1))) label.SelfModulate = SELECTED_COLOR;
				else label.SelfModulate = Colors.White;
			}else if(label.Name.ToString().Contains("Folder")){
				if(label.Name.Equals("Folder" + (Selection - 1))) label.SelfModulate = new Color(0,0.5f,1);
				else label.SelfModulate = new Color(0.5f,0.75f,1);
			}
		}
		if(optionNames.Count > 0){
			LoadLevelVisual();
			if(Game.TotalPlayers == 1){
				soloStatsLabel.Visible = !optionNames[Selection - 1].EndsWith("/"); //Hide if folder
				string currentLevelName = string.Join("", FoldersOpened) + optionNames[Selection - 1];
				float savedRecord = Game.GetSavedLevelRecord(Game.CurrentMode,currentLevelName);
				bool hasRecord = !float.IsNaN(savedRecord);

				switch(Game.CurrentMode){
					case Mode.GameMode.Race:
						soloStatsLabel.Text = $"{soloLevelDataText}\nBest Time: {(hasRecord ? FormatTime(savedRecord) : "--:--.---")}";
						break;
					case Mode.GameMode.Golf:
						soloStatsLabel.Text = $"Par: {soloLevelDataText}\nBest Score: {(hasRecord ? ((int)savedRecord).ToString("0") : "--")}";
						break;
					case Mode.GameMode.Survival:
						soloStatsLabel.Text = $"{soloLevelDataText}\nLongest Time Survived: {(hasRecord ? FormatTime(savedRecord) : "--:--.---")}";
						break;
				}
			}
		}
	}
	
	private void FolderNavigation(bool opening){
		if(opening) FoldersOpened.Add(optionNames[Selection - 1]);
		else FoldersOpened.RemoveAt(FoldersOpened.Count - 1);
		MenuScene.LoadMenu("LevelMenu");
	}

	private void LoadLevelVisual(){
		if(levelVisual != null){
			levelVisual.QueueFree();
			levelVisual = null;
		}

		string currentOption = optionNames[Selection - 1];

		if(!currentOption.EndsWith("/")){
			string folderPath = string.Join("", FoldersOpened);
			string fullPath = Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + folderPath + currentOption;

			try{
				PackedScene scene = GD.Load<PackedScene>(fullPath);
				if(scene != null){
					Node tempLevel = scene.Instantiate();
					if(Game.TotalPlayers == 1){
						switch(Game.CurrentMode){
							case Mode.GameMode.Race:
							case Mode.GameMode.Survival:
								soloLevelDataText = generateMedalText();
								break;

							case Mode.GameMode.Golf:
								soloLevelDataText = tempLevel.HasMeta("Par") ? tempLevel.GetMeta("Par").AsInt32().ToString() : "--";
								break;
						}

						string generateMedalText(){
							if(!tempLevel.HasMeta("Medals")) return "";

							Godot.Collections.Array<float> medals = (Godot.Collections.Array<float>)tempLevel.GetMeta("Medals");
							if(medals.Count < 4) return "";
							string currentLevelName = string.Join("", FoldersOpened) + optionNames[Selection - 1];
							float savedRecord = Game.GetSavedLevelRecord(Game.CurrentMode, currentLevelName);

							//Check if they beat gold (Index 2)
							bool displayDiamond = false;
							switch(Game.CurrentMode){
								case Mode.GameMode.Race:
									displayDiamond = savedRecord <= medals[2];
									break;
								case Mode.GameMode.Survival:
									displayDiamond = savedRecord >= medals[2];
									break;
							}

							string text = "Medals:\n";
							if(displayDiamond){
								text += $"[color=#27F5EE]Diamond: {FormatTime(medals[3])}[/color]\n";
							}

							text += $"[color=gold]Gold: {FormatTime(medals[2])}[/color]\n";
							text += $"[color=silver]Silver: {FormatTime(medals[1])}[/color]\n";
							text += $"[color=#CD7F32]Bronze: {FormatTime(medals[0])}[/color]";

							return text;
						}
					}

					Vector2 levelSize = new Vector2(3840, 2160); 
					Vector2 levelCenter = Vector2.Zero;

					Node bounds = tempLevel.GetNodeOrNull("CameraBoundary");
					if(bounds != null){
						if(bounds is CollisionShape2D colShape && colShape.Shape is RectangleShape2D rectShape){
							levelSize = rectShape.Size;
							levelCenter = colShape.Position;
						}else if(bounds is ReferenceRect refRect){
							levelSize = refRect.Size;
							levelCenter = refRect.Position + (levelSize / 2f);
						}else if(bounds is Control control){
							levelSize = control.Size;
							levelCenter = control.Position + (levelSize / 2f);
						}
					}

					if(levelSize.X == 0 || levelSize.Y == 0) levelSize = new Vector2(3840, 2160);

					Vector2 previewMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
					Vector2 previewMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

					if(previewPoly.Polygon != null && previewPoly.Polygon.Length > 0){
						foreach(Vector2 pt in previewPoly.Polygon){
							previewMin.X = Mathf.Min(previewMin.X, pt.X);
							previewMin.Y = Mathf.Min(previewMin.Y, pt.Y);
							previewMax.X = Mathf.Max(previewMax.X, pt.X);
							previewMax.Y = Mathf.Max(previewMax.Y, pt.Y);
						}
					}else{
						previewMin = Vector2.Zero;
						previewMax = new Vector2(1920, 1080);
					}

					Vector2 previewSize = previewMax - previewMin;
					Vector2 previewCenter = previewMin + (previewSize / 2f);

					Godot.Collections.Array<Node> allNodes = tempLevel.FindChildren("*");
					List<StaticBody2D> validBodies = new List<StaticBody2D>();
					List<Node> levelObjects = new List<Node>();
					Vector2 minVisual = new Vector2(float.MaxValue, float.MaxValue);
					Vector2 maxVisual = new Vector2(float.MinValue, float.MinValue);
					bool hasVisuals = false;
					bool hasInverted = false;
					const int NO_SCRIPT_FLAGS = (int)Node.DuplicateFlags.Groups;
					
					foreach(Node node in allNodes){
						switch(node){
							case StaticBody2D body when node.IsInGroup(Level.GROUP_BAKED_ELEMENT):
								validBodies.Add(body);
								foreach(Node child in body.FindChildren("*", "CollisionPolygon2D")){
									if(child is CollisionPolygon2D colPoly && colPoly.Polygon != null && colPoly.Polygon.Length > 0){
										hasVisuals = true;
										if(colPoly.HasMeta("invert") && (bool)colPoly.GetMeta("invert")){
											hasInverted = true;
										}
										Transform2D absoluteTransform = body.Transform * colPoly.Transform;
										foreach(Vector2 pt in colPoly.Polygon){
											Vector2 absolutePt = absoluteTransform * pt; 
											if(absolutePt.X < minVisual.X) minVisual.X = absolutePt.X;
											if(absolutePt.Y < minVisual.Y) minVisual.Y = absolutePt.Y;
											if(absolutePt.X > maxVisual.X) maxVisual.X = absolutePt.X;
											if(absolutePt.Y > maxVisual.Y) maxVisual.Y = absolutePt.Y;
										}
									}
								}
								break;
							case ItemSpawner itemSpawner:
								Sprite2D spawnerCopy = (Sprite2D)itemSpawner.Duplicate(NO_SCRIPT_FLAGS);
								foreach(Node child in spawnerCopy.GetChildren()){
									child.Free();
								}
								levelObjects.Add(spawnerCopy);
								break;
							case GolfHole hole:
								Node holeCopy = hole.Duplicate(NO_SCRIPT_FLAGS);
								levelObjects.Add(holeCopy);
								break;
							case Sprite2D sprite when node.IsInGroup(Level.GROUP_SPAWN) || node.IsInGroup(Level.GROUP_RESPAWN):
								levelObjects.Add(sprite);
								break;
							default:
								if(node.IsInGroup(Level.GROUP_PREVIEWABLE)){
									Node dumbVisual = node.Duplicate(NO_SCRIPT_FLAGS);
									levelObjects.Add(dumbVisual);
								}
								break;
						}
					}

					if(!hasVisuals){ 
						minVisual = levelCenter - (levelSize / 2f);
						maxVisual = levelCenter + (levelSize / 2f);
					}

					if(!hasInverted){
						Vector2 camMin = levelCenter - (levelSize / 2f);
						Vector2 camMax = levelCenter + (levelSize / 2f);
						minVisual = new Vector2(Mathf.Min(minVisual.X, camMin.X), Mathf.Min(minVisual.Y, camMin.Y));
						maxVisual = new Vector2(Mathf.Max(maxVisual.X, camMax.X), Mathf.Max(maxVisual.Y, camMax.Y));
						levelSize = maxVisual - minVisual;
						levelCenter = minVisual + (levelSize / 2f);
					}

					Vector2 parentScale = previewPoly.Scale;
					if(parentScale.X == 0) parentScale.X = 1f; 
					if(parentScale.Y == 0) parentScale.Y = 1f;

					Vector2 truePreviewSize = previewSize * parentScale;
					float scaleX = truePreviewSize.X / levelSize.X;
					float scaleY = truePreviewSize.Y / levelSize.Y;
					float uniformScale = Mathf.Min(scaleX, scaleY);

					Vector2 wrapperScale = new Vector2(uniformScale / parentScale.X, uniformScale / parentScale.Y);
					float rightEdgeOfLevel = levelCenter.X + (levelSize.X / 2f);
					float positionX = previewMax.X - (rightEdgeOfLevel * wrapperScale.X);
					float positionY = previewCenter.Y - (levelCenter.Y * wrapperScale.Y);

					Node2D levelWrapper = new Node2D();
					levelWrapper.Scale = wrapperScale;
					levelWrapper.Position = new Vector2(positionX, positionY);

					Polygon2D backgroundPoly = new Polygon2D();
					backgroundPoly.ZIndex = -9;
					Vector2[] mappedPreviewPoints = new Vector2[previewPoly.Polygon.Length];
					Vector2 minMapped = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
					Vector2 maxMapped = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

					for(int i = 0; i < previewPoly.Polygon.Length; i++){
						Vector2 pt = previewPoly.Polygon[i];
						Vector2 mappedPt = new Vector2((pt.X - positionX) / wrapperScale.X, (pt.Y - positionY) / wrapperScale.Y);
						mappedPreviewPoints[i] = mappedPt;
						
						minMapped.X = Mathf.Min(minMapped.X, mappedPt.X);
						minMapped.Y = Mathf.Min(minMapped.Y, mappedPt.Y);
						maxMapped.X = Mathf.Max(maxMapped.X, mappedPt.X);
						maxMapped.Y = Mathf.Max(maxMapped.Y, mappedPt.Y);
					}

					backgroundPoly.Polygon = mappedPreviewPoints;

					if(bgGradient != null){
						backgroundPoly.Texture = bgGradient;
						Vector2 texSize = bgGradient.GetSize();
						Vector2[] uvs = new Vector2[mappedPreviewPoints.Length];
						Vector2 mappedSize = maxMapped - minMapped;
						
						for(int i = 0; i < mappedPreviewPoints.Length; i++){
							float u = mappedSize.X != 0 ? (mappedPreviewPoints[i].X - minMapped.X) / mappedSize.X : 0;
							float v = mappedSize.Y != 0 ? (mappedPreviewPoints[i].Y - minMapped.Y) / mappedSize.Y : 0;
							uvs[i] = new Vector2(u * texSize.X, v * texSize.Y);
						}
						backgroundPoly.UV = uvs;
					}else{
						backgroundPoly.Color = Game.CLEAR;
					}

					levelWrapper.AddChild(backgroundPoly);

					foreach(StaticBody2D body in validBodies){
						body.GetParent().RemoveChild(body);
						body.Owner = null;
						foreach(Node child in body.FindChildren("*")){
							child.Owner = null;
						}
						levelWrapper.AddChild(body);
					}

					foreach(Node obj in levelObjects){
						if(obj.GetParent() != null){
							obj.GetParent().RemoveChild(obj);
						}
						obj.Owner = null;
						foreach(Node child in obj.FindChildren("*")){
							child.Owner = null;
						}
						levelWrapper.AddChild(obj);
					}

					levelVisual = levelWrapper;
					previewPoly.AddChild(levelVisual);

					foreach(Node n in tempLevel.FindChildren("*", "Polygon2D")){
						try{
							Variant lineVar = n.Get("line_2d");
							if(lineVar.VariantType == Variant.Type.Object){
								Node lineNode = lineVar.As<Node>();
								if(lineNode != null && !lineNode.IsInsideTree()){
									lineNode.Free();
								}
							}
						}catch{}
					}

					tempLevel.QueueFree();
				}
			}catch(Exception ex){
				GD.PrintErr("Failed to load visual for level: " + currentOption + "\nError: " + ex.Message);
			}
		}
	}


	private static string FormatTime(float timeInSeconds){
		int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
		int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
		int milliseconds = Mathf.FloorToInt((timeInSeconds - Mathf.Floor(timeInSeconds)) * 1000f);
		return $"{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
	}
}