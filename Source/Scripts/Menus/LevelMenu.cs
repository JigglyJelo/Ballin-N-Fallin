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

	public override void _Ready(){
		base._Ready();
		Tour.ResetPlayerScores();
		if(FoldersOpened == null) FoldersOpened = FoldersOpened = new List<string> {""};
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
	//Either starts the level that's selected or opens the folder that's selected
	protected override void MenuChoose(int choice){
		SFX.Play("Confirm");
		foreach(Node node in selectionsNode.GetChildren()){
			Label label;
			if(node is Label) label = node as Label;
			else break;
			//Start Level
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
				
			//Open Folder
			}else if(label.Name.ToString().Equals("Folder" + (choice - 1)) && (!GolfCup.IsCup || !(optionNames[choice - 1].Contains("Cup")))){
				FolderNavigation(true);
			//Start Golf Cup
			}else if(label.Name.ToString().Equals("Folder" + (choice - 1)) && GolfCup.IsCup && optionNames[choice - 1].Contains("Cup")){
				GolfCup.PrepareCup(FoldersOpened);
				MenuScene.MenuBackgroundFadeout();
				SceneTransitioner.SwitchToScene(Game.SceneType.Game);
			}
		}
		
	}
	//Either returns back to the last Menu if not in a folder else exits the folder
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
	//Colors current selection green
	
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
		if(optionNames.Count > 0) LoadLevelVisual();
	}
	
	///<summary>Opens or closes a folder.</summary><param name="opening">true to open, false to close.</param>
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

		if (!currentOption.EndsWith("/")){
			string folderPath = string.Join("", FoldersOpened);
			string fullPath = Game.LEVELS_PATH + Mode.EnumToString(Game.CurrentMode) + " Levels/" + folderPath + currentOption;

			try{
				PackedScene scene = GD.Load<PackedScene>(fullPath);
				if(scene != null){
					Node tempLevel = scene.Instantiate();

					//Get CameraBoundary Data
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

					Polygon2D previewPoly = GetNode<Polygon2D>("LevelPreview");
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

					//Get bodies and bounds
					Godot.Collections.Array<Node> staticBodies = tempLevel.FindChildren("*", "StaticBody2D");
					List<StaticBody2D> validBodies = new List<StaticBody2D>();

					Vector2 minVisual = new Vector2(float.MaxValue, float.MaxValue);
					Vector2 maxVisual = new Vector2(float.MinValue, float.MinValue);
					bool hasVisuals = false;
					bool hasInverted = false;

					foreach(Node node in staticBodies){
						if(node.IsInGroup("BakedLevelGeometry")){
							StaticBody2D body = node as StaticBody2D;
							validBodies.Add(body);

							// Search this body's direct collision polygons
							foreach(Node child in body.FindChildren("*", "CollisionPolygon2D")){
								if(child is CollisionPolygon2D colPoly && colPoly.Polygon != null && colPoly.Polygon.Length > 0){
									hasVisuals = true;
									if(colPoly.HasMeta("invert") && (bool)colPoly.GetMeta("invert")){
										hasInverted = true;
									}

									// Combine the transforms so we get the absolute point in Level Space
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
						}
					}

					if(!hasVisuals){ 
						// Minor correction: factored in levelCenter rather than assuming 0,0
						minVisual = levelCenter - (levelSize / 2f);
						maxVisual = levelCenter + (levelSize / 2f);
					}

					// Combine polygon limits and camera boundaries if there are no inverted polygons
					if(!hasInverted){
						Vector2 camMin = levelCenter - (levelSize / 2f);
						Vector2 camMax = levelCenter + (levelSize / 2f);

						minVisual = new Vector2(Mathf.Min(minVisual.X, camMin.X), Mathf.Min(minVisual.Y, camMin.Y));
						maxVisual = new Vector2(Mathf.Max(maxVisual.X, camMax.X), Mathf.Max(maxVisual.Y, camMax.Y));

						// Update levelSize and levelCenter so the final scale wrapper fits this combined max size
						levelSize = maxVisual - minVisual;
						levelCenter = minVisual + (levelSize / 2f);
					}

					//Background
					Node2D levelWrapper = new Node2D();

					Polygon2D backgroundPoly = new Polygon2D();
					string bgPath = $"res://Assets/Gradients/{Mode.EnumToString(Game.CurrentMode)}.tres";
					if(ResourceLoader.Exists(bgPath)){
						GradientTexture2D tex = GD.Load<GradientTexture2D>(bgPath);
						backgroundPoly.Texture = tex;

						// Assign UVs so the gradient scales perfectly across your background
						Vector2 texSize = tex.GetSize();
						backgroundPoly.UV = new Vector2[]{
							new Vector2(0, 0),
							new Vector2(texSize.X, 0),
							new Vector2(texSize.X, texSize.Y),
							new Vector2(0, texSize.Y)
						};
					}

					backgroundPoly.Polygon = new Vector2[]{
						new Vector2(minVisual.X, minVisual.Y), //Top Left
						new Vector2(maxVisual.X, minVisual.Y), //Top Right
						new Vector2(maxVisual.X, maxVisual.Y), //Bottom Right
						new Vector2(minVisual.X, maxVisual.Y)  //Bottom Left
					};

					// Add background first so it draws strictly behind everything
					levelWrapper.AddChild(backgroundPoly);

					// Re-parent the bodies into the wrapper cleanly
					foreach(StaticBody2D body in validBodies){
						body.GetParent().RemoveChild(body);
						body.Owner = null;
						foreach(Node child in body.FindChildren("*")){
							child.Owner = null;
						}
						levelWrapper.AddChild(body);
					}


					// Get the parent's scale (LevelPreview might be stretched in the editor)
					Vector2 parentScale = previewPoly.Scale;
					if (parentScale.X == 0) parentScale.X = 1f; // Prevent divide by zero
					if (parentScale.Y == 0) parentScale.Y = 1f;

					// Calculate the true visual size of the preview polygon on screen
					Vector2 truePreviewSize = previewSize * parentScale;

					// Find the uniform scale required to fit the level perfectly
					float scaleX = truePreviewSize.X / levelSize.X;
					float scaleY = truePreviewSize.Y / levelSize.Y;
					float uniformScale = Mathf.Min(scaleX, scaleY);

					// Counter-scale the wrapper so it doesn't inherit parent stretching
					Vector2 wrapperScale = new Vector2(uniformScale / parentScale.X, uniformScale / parentScale.Y);

					// Calculate Right-Alignment for X and Center-Alignment for Y
					float rightEdgeOfLevel = levelCenter.X + (levelSize.X / 2f);

					// Position X = Right edge of preview polygon minus the scaled right edge of the level
					float positionX = previewMax.X - (rightEdgeOfLevel * wrapperScale.X);

					// Position Y = Center of preview polygon minus the scaled center of the level
					float positionY = previewCenter.Y - (levelCenter.Y * wrapperScale.Y);

					levelWrapper.Scale = wrapperScale;
					levelWrapper.Position = new Vector2(positionX, positionY);

					levelVisual = levelWrapper;
					previewPoly.AddChild(levelVisual);

					tempLevel.QueueFree();
				}
			}catch(Exception ex){
				GD.PrintErr("Failed to load visual for level: " + currentOption + "\nError: " + ex.Message);
			}
		}
	}
}