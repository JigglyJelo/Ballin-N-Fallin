using Godot;
using System;
using System.Collections.Generic;

public partial class StraightMovingBackgroundElement : MovingBackgroundElement{
    [Export]
	protected Vector2 startingPosition;
	[Export]
	protected Vector2 movementDirection;
	protected Random random = new Random();
	protected Vector2 movementVector;
    public override void _Ready(){
		movementVector = movementDirection.Normalized();
        movementVector *= minSpeed + (random.NextSingle() * (maxSpeed - minSpeed));
        List<Sprite2D> movingSpritesList = new List<Sprite2D>();
		//Moving Horizontal
        if(movementDirection.X > movementDirection.Y){
			float size = movingTexture.GetWidth() * textureScale;
			startingPosition -= new Vector2(size,0);
			for(float xPos = startingPosition.X; xPos < 3840; xPos += size){
				CreateNewSprite(new Vector2(xPos,startingPosition.Y));
			}
		//Moving Vertical
		}else if(movementDirection.X < movementDirection.Y){
			float size = movingTexture.GetHeight() * textureScale;
			startingPosition -= new Vector2(0,size);
			for(float yPos = startingPosition.Y; yPos < 2160; yPos += size){
				CreateNewSprite(new Vector2(startingPosition.X,yPos));
			}
		}
    }

    public override void _Process(double delta){
        foreach(Node2D sprite in movingBackgroundElements){
			float fDelta = (float)delta;
			sprite.Position += movementVector * fDelta;
			//If offscreen loop back
			if((sprite.Position.X > 3840 && movementVector.X > 0) || (sprite.Position.X > 2160 && movementVector.Y > 0) || (sprite.Position.X < -3840 && movementVector.X < 0) || (sprite.Position.Y < -2160 && movementVector.Y < 0)){
				sprite.Position = startingPosition + (movementVector * fDelta) + movementDirection.Normalized();
			}
		}
	}
}