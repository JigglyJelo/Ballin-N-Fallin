using Godot;
using System.Collections.Generic;

public partial class Crown : RigidBody2D{

	public override void _Ready(){
		Sprite2D spriteNode = GetNode<Sprite2D>("Sprite2D");
		int resolution = Game.Resolution >= 720 ? Game.Resolution : 720;
		spriteNode.Texture = GD.Load<Texture2D>("res://Assets/Sprites/Mode Stuff/Crown the King/GroundCrowns/Crown" + resolution + ".png");
		spriteNode.RegionRect = new Rect2(0,0,spriteNode.Texture.GetWidth()*2,spriteNode.Texture.GetHeight());
		float textureScale = 2160f / resolution;
		spriteNode.Scale = new Vector2(textureScale,textureScale);
		spriteNode.Visible = true;
		//foreach(Player player in Game.Players) AddCollisionExceptionWith(player.Rb); Moved to level spawn players function
		//Gets random spawn point if more than one
        List<Node2D> respawnPoints = new List<Node2D>();
		foreach(Node node in Mode.ModeNode.GetNode<Node2D>("Level").GetChildren()){ //Change to Level.LevelNode
			if(node.IsInGroup("Respawn")) respawnPoints.Add(node as Node2D);
		}
        Node2D respawnPoint = respawnPoints[Game.Random.Next(0,respawnPoints.Count)];
		GlobalPosition = respawnPoint.GlobalPosition;
	}

	public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(body.IsInGroup("Player") && Online.IsHost()){
			Player player = body.GetParent() as Player;
			//player.Rpc(nameof(player.GiveIt));
			CTK ctkNode = Mode.ModeNode as CTK;
			ctkNode.Rpc(nameof(ctkNode.DeleteCrown));
			ctkNode.Rpc(nameof(ctkNode.SyncKing),player.Id);
		}
	}
}