using Godot;
using System.Collections.Generic;

public partial class Sand : Area2D{
	public Dictionary<byte,float> Timers = new Dictionary<byte, float>();
	private CollisionShape2D collisionShape;
	private static readonly Color SAND_BOTTOM_COLOR = Color.Color8(220,175,95);
	public override void _Ready(){
		//Create visuals
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		RectangleShape2D shape = collisionShape.Shape as RectangleShape2D;
		Sprite2D sandTopSprite = new Sprite2D();
		sandTopSprite.Texture = GD.Load<Texture2D>("res://Assets/Sprites/Level Stuff/SandTop.png");
		sandTopSprite.TextureRepeat = TextureRepeatEnum.Enabled;
		sandTopSprite.RegionEnabled = true;
		sandTopSprite.LightMask = 0b11;

		/* Shadow disapears at edge of sand this tried to fix it but didnt so thats getting put off for later
		Area2D shadowArea = new Area2D();
		AddChild(shadowArea);
		CollisionShape2D shadowShape = new CollisionShape2D();
		shadowShape.Shape = new RectangleShape2D();
		(shadowShape.Shape as RectangleShape2D).Size = new Vector2(shape.Size.X, shape.Size.Y);
		shadowArea.AddChild(shadowShape);
		shadowArea.CollisionLayer = 0b01;
		shadowArea.CollisionMask = 0b01;
		*/

		float spriteY = sandTopSprite.Texture.GetSize().Y;
		if(shape.Size.Y >= spriteY){
			sandTopSprite.RegionRect = new Rect2(0, 2, shape.Size.X, spriteY);
		}else{
			sandTopSprite.RegionRect = new Rect2(Vector2.Zero, shape.Size.X, shape.Size.Y);
		}
		const float MIP_MAP_BUFFER = 4;
		const float BOTTOM = 32;
		sandTopSprite.ZIndex = -1;
		sandTopSprite.GlobalPosition -= new Vector2(0,(shape.Size.Y/2)-BOTTOM);
		collisionShape.AddChild(sandTopSprite);
		bool needsBottom = shape.Size.Y > spriteY-BOTTOM;
		if(needsBottom){
			Polygon2D bottomPolygon = new Polygon2D();
			bottomPolygon.Polygon = new Vector2[]{
				new Vector2(-shape.Size.X/2,(shape.Size.Y/2)-shape.Size.Y+spriteY-BOTTOM-1-MIP_MAP_BUFFER),
				new Vector2(shape.Size.X/2,(shape.Size.Y/2)-shape.Size.Y+spriteY-BOTTOM-1-MIP_MAP_BUFFER),
				new Vector2(shape.Size.X/2,shape.Size.Y/2),
				new Vector2(-shape.Size.X/2,shape.Size.Y/2)
			};
			bottomPolygon.Color = SAND_BOTTOM_COLOR;
			bottomPolygon.ZIndex = -1;
			collisionShape.AddChild(bottomPolygon);
		}
	}

	public override void _Draw(){
		DrawOutline(collisionShape,(GetParent() as Level).OutlineColorOverride,Level.OUTLINE_WIDTH-1);
		RectangleShape2D shape = collisionShape.Shape as RectangleShape2D;
		//Must create new Rectangle shape or else it will overwrite the saved one in the sand scene causing it to shrink each time the level is loaded
		RectangleShape2D newRectangleShape = new RectangleShape2D();
		newRectangleShape.Size = new Vector2(shape.Size.X - 64, shape.Size.Y);
		collisionShape.Shape = newRectangleShape;
	}

    public override void _PhysicsProcess(double delta){
		//For each Player in the List their sand timer is increased while not stuck but still in the sand
		//While the player is stuck the timer is set to 0
		//If the sand timer is greater than 0.125s then the player gets stuck again in the sand allowing them to move a little in it
		foreach(KeyValuePair<byte,float> keyValuePair in Timers){
			Player player = Game.Players[keyValuePair.Key-1];
			Timers[player.Id] += (float)delta;
			if(!player.Rb.Freeze && Timers[player.Id] >= 0.0625f && Online.IsHost() && player.Rb.LinearVelocity.Y >= 0) Rpc(nameof(StickPlayer),player.Id);
			else if(player.Rb.Freeze) Timers[player.Id] = 0;
			player.FrozenTimer = 0;
		}
    }

	public void _on_body_exited(PhysicsBody2D body){
		//Remove the player from the List and Dictionary
		if(Online.IsHost() && body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			if (Timers.ContainsKey(player.Id)){
				Timers.Remove(player.Id);
				GD.Print("Removed " + player.Id);
				Rpc(nameof(UnstickPlayer), player.Id);
			}
		}
	}

    public void _on_body_entered(PhysicsBody2D body){
		//When player enters sand they get frozen and added to a List of all stuck players
		//A dictionary entry is created for them with a timer to prevent getting stuck when leaving or not getting stuck when launching into sand when still in it
		if(body.IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			if(Timers.ContainsKey(player.Id)){
				GD.PrintErr("Sand already has player");
				return;
			}
			if(Online.IsHost()){
				Rpc(nameof(StickPlayer), player.Id);
				Timers.Add(player.Id, 0.125f);
			}
			SFX.Play("Sand",player.Rb.GlobalPosition);
			player.SpawnPoint = player.Rb.GlobalPosition;
		}else body.SetDeferred("freeze", true);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StickPlayer(byte id){
		Player player = Game.Players[id-1];
		player.Rb.SetDeferred("freeze",true);
		player.CanLaunch = true;
		player.CanSlam = true;
		player.FrozenTimer = float.NegativeInfinity;
		player.Rb.LinearVelocity = Vector2.Zero;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void UnstickPlayer(byte id){
		try{
			Player player = Game.Players[id - 1];
			player.Rb.SetDeferred("freeze", false);
		}catch{}
	}

	private void DrawOutline(CollisionShape2D collisionShape, Color color, float width){
		RectangleShape2D shape = collisionShape.Shape as RectangleShape2D;
		Vector2[] points = {
			new Vector2(collisionShape.Position.X - ((shape.Size.X - 5)/2f),collisionShape.Position.Y - (shape.Size.Y/2f)), //Top left
			new Vector2(collisionShape.Position.X - ((shape.Size.X - 5)/2f),collisionShape.Position.Y + (shape.Size.Y/2f)), //Bottom left
			new Vector2(collisionShape.Position.X + ((shape.Size.X - 5)/2f),collisionShape.Position.Y + (shape.Size.Y/2f)), //Bottom right
			new Vector2(collisionShape.Position.X + ((shape.Size.X - 5)/2f),collisionShape.Position.Y - (shape.Size.Y/2f)) //Top right
		};
		DrawPolyline(points, color, width, true);
	}
}