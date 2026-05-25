using Godot;

public partial class InterpolatedBody : RigidBody2D{
    // Export the Visual Node
    [Export] public Node2D VisualsNode;

    // Network Variables
    public Vector2 NetworkPosition = Vector2.Inf;
    public Vector2 NetworkVelocity = Vector2.Inf;
    public float NetworkAngularVelocity = Mathf.Inf;
    private const float NETWORK_LERP_WEIGHT = 0.5f;
    private const float MIN_NET_INTERPOLATE_DISTANCE = 30;

    // Variables to track physics frames
    private Vector2 previousPosition;
    private Vector2 currentPosition;
    private float previousRotation;
    private float currentRotation;
    private bool skipInterpolation = false;
    private const float MAX_INTERPOLATE_DISTANCE = 2000;

    public override void _Ready(){
        ProcessPriority = 999;
        ProcessPhysicsPriority = 999;
        
        if(VisualsNode != null){
            VisualsNode.TopLevel = true;
            previousPosition = GlobalPosition;
            currentPosition = GlobalPosition;
            previousRotation = GlobalRotation;
            currentRotation = GlobalRotation;
            VisualsNode.GlobalPosition = GlobalPosition;
        }
    }

    public override void _Process(double delta){
        if(VisualsNode != null){
            
            // If frozen, _IntegrateForces does not run. We must read GlobalPosition 
            // directly so the visual node doesn't get left behind when teleported!
            if(Freeze){
                currentPosition = GlobalPosition;
                currentRotation = GlobalRotation;
                previousPosition = GlobalPosition;
                previousRotation = GlobalRotation;
                skipInterpolation = true;
            }

            if(skipInterpolation || previousPosition.DistanceSquaredTo(currentPosition) > MAX_INTERPOLATE_DISTANCE*MAX_INTERPOLATE_DISTANCE){
                VisualsNode.GlobalPosition = currentPosition;
                VisualsNode.GlobalRotation = currentRotation;
                skipInterpolation = false; 
            }else{
                // Reverted back to the built-in fraction. Since we capture state in _IntegrateForces now, this works perfectly.
                float fraction = (float)Engine.GetPhysicsInterpolationFraction();
                VisualsNode.GlobalPosition = previousPosition.Lerp(currentPosition, fraction);
                VisualsNode.GlobalRotation = Mathf.LerpAngle(previousRotation, currentRotation, fraction);
            }
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state){
        base._IntegrateForces(state);
        
        if(!Mathf.IsInf(NetworkPosition.X)){
            Transform2D newTransform = state.Transform;
            
            float distanceSquared = newTransform.Origin.DistanceSquaredTo(NetworkPosition);
            if(distanceSquared > MAX_INTERPOLATE_DISTANCE*MAX_INTERPOLATE_DISTANCE || distanceSquared < MIN_NET_INTERPOLATE_DISTANCE*MIN_NET_INTERPOLATE_DISTANCE){
                newTransform.Origin = NetworkPosition;
                currentPosition = newTransform.Origin;
                previousPosition = newTransform.Origin;
                currentRotation = newTransform.Rotation;
                previousRotation = newTransform.Rotation;
                SkipInterpolation();
            }else{
                newTransform.Origin = newTransform.Origin.Lerp(NetworkPosition, NETWORK_LERP_WEIGHT);
            }
            
            state.Transform = newTransform;
            NetworkPosition = Vector2.Inf;
        }

        if(!Mathf.IsInf(NetworkVelocity.X)){
            state.LinearVelocity = NetworkVelocity;
            NetworkVelocity = Vector2.Inf;
        }

        if(!Mathf.IsInf(NetworkAngularVelocity)){
            state.AngularVelocity = NetworkAngularVelocity;
            NetworkAngularVelocity = Mathf.Inf;
        }

        // FIX 2: Always capture the current state first!
        previousPosition = currentPosition;
        currentPosition = state.Transform.Origin;
        previousRotation = currentRotation;
        currentRotation = state.Transform.Rotation;

        // If skipping interpolation, instantly align 'previous' with 'current' 
        // so there is no distance to interpolate.
        if (skipInterpolation) {
            previousPosition = currentPosition;
            previousRotation = currentRotation;
        }
    }

    public void SkipInterpolation(){
        skipInterpolation = true;
    }
}