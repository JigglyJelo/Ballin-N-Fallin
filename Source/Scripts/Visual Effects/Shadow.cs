using Godot;
using System;

public partial class Shadow : Node2D{
    [Export]
    private float widthMultiplier = 1;
    private RayCast2D rayCast;
    private PointLight2D shadow;
    private const float HALF_PI = (float)(Math.PI / 2.0);
    private float invTargetY;
    private Vector2 tmpScale = Vector2.One;

    public override void _Ready(){
        rayCast = GetNode<RayCast2D>("RayCast2D");
        shadow = GetNode<PointLight2D>("PointLight2D");
        rayCast.TargetPosition = new Vector2(0, 1000 * widthMultiplier);
        invTargetY = 1f / rayCast.TargetPosition.Y;
    }

    public override void _PhysicsProcess(double delta){
        if(!rayCast.IsColliding()){
            shadow.Visible = false;
            return;
        }

        // collision is true
        shadow.Visible = true;
        Vector2 collisionPoint = rayCast.GetCollisionPoint();
        Vector2 normal = rayCast.GetCollisionNormal();

        shadow.GlobalTransform = new Transform2D(MathF.Atan2(normal.Y, normal.X) + HALF_PI, collisionPoint);

        // scale based on height
        float distance = collisionPoint.Y - rayCast.GlobalPosition.Y;
        float scale = 1 - distance * invTargetY;
        scale = Mathf.Clamp(scale, 0, 1);

        // width factor via normal.x instead of Sin(Atan2+π/2)
        tmpScale.X = scale * (1 + 0.25f * MathF.Abs(normal.X)) * widthMultiplier;
        tmpScale.Y = scale;

        shadow.Scale = tmpScale;
    }
}