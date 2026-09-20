using Godot;

public partial class LevelBackground : CanvasLayer{
	private Node2D[] backgroundLayers;
	private Vector2[] basePositions;
	private Vector2[] baseScales;
	private float[] baseDepths;
	private TextureRect backgroundColor;
	
	public override void _Ready(){
		//Scale background based on resolution
		Scale = Game.ResolutionScaleVector2;
		//Get all the background layers into an array
		Godot.Collections.Array<Node> layers = GetNode("BackgroundLayers").GetChildren();
		
		backgroundLayers = new Node2D[layers.Count];
		basePositions = new Vector2[layers.Count];
		baseScales = new Vector2[layers.Count];
		baseDepths = new float[layers.Count];
		
		for(int i = 0; i < backgroundLayers.Length; i++){
			if(layers[i] is Node2D layer){
				backgroundLayers[i] = layer;
				basePositions[i] = layer.Position;
				baseScales[i] = layer.Scale;
				baseDepths[i] = layer.GetMeta("ParallaxDepth",0f).AsSingle();
			}
		}
		
		backgroundColor = GetNode<TextureRect>("Gradient");
		//Load mode's background Gradient
		if(backgroundColor.Texture == null){
			string modeName = Mode.EnumToString(Game.CurrentMode);
			string filePath = ("res://Assets/Gradients/" + modeName + ".tres").Replace(".remap","");
			if(ResourceLoader.Exists(filePath)){
				backgroundColor.Texture = GD.Load<GradientTexture2D>(filePath);
				backgroundColor.Texture.ResourceName = modeName + " Gradient";
			}
		}
		//Additional tinting
		foreach(Node2D layer in backgroundLayers){
			if(layer.Modulate.Equals(Colors.White)){
				float lightness = layer.GetMeta("Lightness",0f).AsSingle();
				layer.Modulate = Level.LevelNode.InsideColorOverride.Lightened(lightness);
			}else{
				foreach(Node layerElement in layer.GetChildren()){
					if(layerElement is Node2D layerElement2D){
						float lightness = layerElement.GetMeta("Lightness", 0f).AsSingle();
						layerElement2D.Modulate = Level.LevelNode.InsideColorOverride.Lightened(lightness);
					}
				}
			}
		}
	}

	public override void _Process(double delta){
		if(DynamicCamera.CameraNode == null || !DynamicCamera.CameraNode.HasValidBounds) return;

		Vector2 camPos = DynamicCamera.CameraNode.GlobalPosition;
		Vector2 camZoom = DynamicCamera.CameraNode.Zoom;
		Vector2 levelCenter = DynamicCamera.CameraNode.LevelBoundsCenter;
		Vector2 minZoom = DynamicCamera.CameraNode.AbsoluteMinZoomFloor;

		Vector2 zoomRatio = new Vector2(
			minZoom.X > 0 ? camZoom.X / minZoom.X : 1f,
			minZoom.Y > 0 ? camZoom.Y / minZoom.Y : 1f
		);

		Vector2 worldOffset = camPos - levelCenter;
		Vector2 screenSpaceOffset = (worldOffset * camZoom) / Scale;
		Vector2 localViewportCenter = (GetViewport().GetVisibleRect().Size / 2f) / Scale;

		for(int i = 0; i < backgroundLayers.Length; i++){
			Node2D layer = backgroundLayers[i];
			if(layer == null) continue;
			
			float depth = baseDepths[i];
			
			// Zoom
			Vector2 targetScale = baseScales[i] * new Vector2(
				Mathf.Lerp(1f, zoomRatio.X, depth),
				Mathf.Lerp(1f, zoomRatio.Y, depth)
			);
			layer.Scale = targetScale;

			// Zoom Centering Pivot
			Vector2 scaleRatio = targetScale / baseScales[i];
			Vector2 zoomCenteringOffset = localViewportCenter - (localViewportCenter * scaleRatio);

			// Motion Shift
			Vector2 motionShift = -screenSpaceOffset * depth;

			layer.Position = basePositions[i] + zoomCenteringOffset + motionShift;
		}
	}
}