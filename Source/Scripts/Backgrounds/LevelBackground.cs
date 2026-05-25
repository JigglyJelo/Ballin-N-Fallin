using Godot;

public partial class LevelBackground : CanvasLayer{
	private Node2D[] backgroundLayers;
	private TextureRect backgroundColor;
	public override void _Ready(){
		//Scale background based on resolution
		Scale = Game.ResolutionScaleVector2;
		//Get all the background layers into an array
		Godot.Collections.Array<Node> layers = GetNode("BackgroundLayers").GetChildren();
		backgroundLayers = new Node2D[layers.Count];
		for(int i = 0; i < backgroundLayers.Length; i++){
			if(layers[i] is Node2D layer){
				backgroundLayers[i] = layer;
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
		//Honestly not really even sure how this works anymore
		foreach(Node2D layer in backgroundLayers){
			if(layer.Modulate.Equals(Colors.White)){
				float lightness = 0;
				if(layer.HasMeta("Lightness")) lightness = (float)layer.GetMeta("Lightness");
				layer.Modulate = Level.LevelNode.InsideColorOverride.Lightened(lightness);
			}else{
				foreach(Node layerElement in layer.GetChildren()){
					if(layerElement is Node2D layerElement2D){
						float lightness = 0;
						if(layerElement2D.HasMeta("Lightness")) lightness = (float)layer.GetMeta("Lightness");
						layerElement2D.Modulate = Level.LevelNode.InsideColorOverride.Lightened(lightness);
					}
				}
			}
		}
	}
}
