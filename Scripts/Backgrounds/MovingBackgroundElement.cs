using System.Collections.Generic;
using Godot;

public partial class MovingBackgroundElement : Node{
	[Export]
	protected float minSpeed = 100;
	[Export]
	protected float maxSpeed = 100;
	[Export]
	protected Texture2D movingTexture;
	[Export]
	protected float textureScale = 1;
	protected List<Node2D> movingBackgroundElements = new List<Node2D>();
	protected void CreateNewSprite(Vector2 position){
		Sprite2D sprite = new Sprite2D();
		sprite.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		sprite.Centered = false;
		sprite.Position = position;
		sprite.Texture = movingTexture;
		sprite.Scale = new Vector2(textureScale+0.01f,textureScale+0.01f);
		movingBackgroundElements.Add(sprite as Node2D);
		AddChild(sprite);
	}
}