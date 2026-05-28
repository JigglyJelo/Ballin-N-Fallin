using Godot;
using System.Collections.Generic;

public partial class Booster : Area2D{
	[Export]
	private float speed = 1000;
	[Export]
	private Vector2 direction = Vector2.Left;
	private Vector2 directionHorizontal;
	private List<RigidBody2D> nodes = new List<RigidBody2D>();
	private Vector2 boosterForce;
	private Polygon2D visualPolygon;

    public override void _Ready(){
        direction = direction.Normalized();
		directionHorizontal = new Vector2(direction.X,0);
		visualPolygon = GetNode<Polygon2D>("Polygon2D");
		CollisionPolygon2D collisionPolygon = GetNodeOrNull<CollisionPolygon2D>("CollisionPolygon2D");
		if(collisionPolygon != null) collisionPolygon.Polygon = visualPolygon.Polygon;
		boosterForce = direction * speed;
    }

    public override void _PhysicsProcess(double delta){
		if(nodes.Count > 0){
			foreach(RigidBody2D rb in nodes){
				rb.LinearVelocity += boosterForce * (float)delta;
			}
		}
    }

    public override void _Process(double delta){
        visualPolygon.TextureOffset -= directionHorizontal * speed * (float)delta;
    }

    public void _on_body_entered(PhysicsBody2D body) {	
		if(!(body is StaticBody2D)) nodes.Add(body as RigidBody2D);
	}

	public void _on_body_exited(PhysicsBody2D body) {	
		if(!(body is StaticBody2D)) nodes.Remove(body as RigidBody2D);
	}
}