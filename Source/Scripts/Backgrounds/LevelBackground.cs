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
		//Handles the parallax scaling
		Vector2 camPos = DynamicCamera.CameraNode.GlobalPosition;
		Vector2 camZoom = DynamicCamera.CameraNode.Zoom;
		Vector2 levelCenter = DynamicCamera.CameraNode.LevelBoundsCenter;
		Vector2 minZoom = DynamicCamera.CameraNode.AbsoluteMinZoomFloor;

		Vector2 zoomRatio = new Vector2(
			minZoom.X > 0 ? camZoom.X / minZoom.X : 1f,
			minZoom.Y > 0 ? camZoom.Y / minZoom.Y : 1f
		);
		// Prevent floating-point problems by snapping to 0 when fully zoomed out
		Vector2 parallaxIntensity = new Vector2(
			zoomRatio.X <= 1.001f ? 0f : 1f - (1f / zoomRatio.X),
			zoomRatio.Y <= 1.001f ? 0f : 1f - (1f / zoomRatio.Y)
		);

		// Adjust position so the layer visually scales from the center of the screen
		Vector2 screenSpaceOffset = ((camPos - levelCenter) * camZoom) / Scale;
		Vector2 localViewportSize = GetViewport().GetVisibleRect().Size / Scale;
		Vector2 localViewportCenter = localViewportSize / 2f;

		for(int i = 0; i < backgroundLayers.Length; i++){
			Node2D layer = backgroundLayers[i];
			if(layer == null) continue;

			float depth = baseDepths[i];
			
			Vector2 targetScale = baseScales[i] * new Vector2(
				Mathf.Lerp(1f, zoomRatio.X, depth),
				Mathf.Lerp(1f, zoomRatio.Y, depth)
			);
			layer.Scale = targetScale;

			Vector2 zoomCenteringOffset = localViewportCenter - (localViewportCenter * (targetScale / baseScales[i]));
			Vector2 motionShift = -screenSpaceOffset * depth * parallaxIntensity;
			Vector2 finalPos = basePositions[i] + zoomCenteringOffset + motionShift;

			// Clamp position to ensure the layer's sprites physical edges never enter the viewport
			if(layer is Sprite2D sprite){
				if(sprite.Texture != null){
					// Get unscaled bounds that automatically account for region and centering
					Rect2 localRect = sprite.GetRect();

					float leftBound = localRect.Position.X * targetScale.X;
					float rightBound = localRect.End.X * targetScale.X;
					float topBound = localRect.Position.Y * targetScale.Y;
					float bottomBound = localRect.End.Y * targetScale.Y;
					// Calculate the absolute position limits needed to cover the screen
					float minX = localViewportSize.X - rightBound;
					float maxX = -leftBound;
					float minY = localViewportSize.Y - bottomBound;
					float maxY = -topBound;

					if(minX <= maxX) finalPos.X = Mathf.Clamp(finalPos.X, minX, maxX);
					if(minY <= maxY) finalPos.Y = Mathf.Clamp(finalPos.Y, minY, maxY);
				}
			}
			layer.Position = finalPos;
		}
	}
}