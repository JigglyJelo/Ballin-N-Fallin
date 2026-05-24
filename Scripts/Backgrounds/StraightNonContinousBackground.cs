using Godot;

public partial class StraightNonContinousBackground : StraightMovingBackgroundElement{
    [Export]
    private int amount = 1;
    [Export]
    private Vector2I spawnRange = new Vector2I(0,0);
    [Export]
    private Vector2 scaleRange = Vector2.One;
    [Export]
    private bool flippable = false;
    private float[] speeds;
    private int xMin,xMax,yMin,yMax;

    public override void _Ready(){
        movementVector = movementDirection.Normalized();
        Vector2I startingRange = new Vector2I();
        //Moving Horizontal right
        if(movementVector.X > movementVector.Y){
			float size = movingTexture.GetWidth() * textureScale;
			startingPosition -= new Vector2(size,0);
            startingRange = new Vector2I(3840,0);
		//Moving Vertical down
		}else if(movementVector.X < movementVector.Y){
			float size = movingTexture.GetHeight() * textureScale;
			startingPosition -= new Vector2(0,size);
            startingRange = new Vector2I(0,2160);
		}
        if(spawnRange.X > 0){
            xMin = 0;
            xMax = spawnRange.X;
        }else{
            xMin = spawnRange.X;
            xMax = 0;
        }
        if(spawnRange.Y > 0){
            yMin = 0;
            yMax = spawnRange.Y;
        }else{
            yMin = spawnRange.Y;
            yMax = 0;
        }
        for(int i = 0; i < amount; i++){
            CreateNewSprite(new Vector2(random.Next(0,startingRange.X),random.Next(0,startingRange.Y)) + startingPosition + new Vector2(random.Next(xMin,xMax),random.Next(yMin,yMax)));
        }
        speeds = new float[movingBackgroundElements.Count];
        for(int i = 0; i < speeds.Length; i++){
            speeds[i] = minSpeed + (random.NextSingle() * (maxSpeed - minSpeed));
            float scale = Mathf.Lerp(scaleRange[0],scaleRange[1],speeds[i]/maxSpeed);
            movingBackgroundElements[i].Scale = textureScale * new Vector2(scale,scale);
        }
    }

    public override void _Process(double delta){
        for(int i = 0; i < movingBackgroundElements.Count; i++){
            Node2D node = movingBackgroundElements[i];
			float fDelta = (float)delta;
			node.Position += movementVector * fDelta * speeds[i];
			//If offscreen loop back
			if((node.Position.X > 3840 && movementVector.X > 0) || (node.Position.X > 2160 && movementVector.Y > 0) || (node.Position.X < -3840 && movementVector.X < 0) || (node.Position.Y < -2160 && movementVector.Y < 0)){
                speeds[i] = minSpeed + (random.NextSingle() * (maxSpeed - minSpeed));
                float scale = Mathf.Lerp(scaleRange[0],scaleRange[1],speeds[i]/maxSpeed);
                node.Scale = textureScale * new Vector2(scale,scale);
                if(flippable && node is Sprite2D sprite) sprite.FlipH = random.Next(0,2) == 1;
				node.Position = startingPosition + new Vector2(random.Next(xMin,xMax),random.Next(yMin,yMax));
			}
		}
	}
}