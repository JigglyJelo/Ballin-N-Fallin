using Godot;

public partial class DynamicCamera : Camera2D{
    private float moveSpeed = 3f;
    
    //Zoom speeds
    private float zoomOutSpeed = 3f;
    private float zoomInSpeed = 1.5f;   

    private float minZoomMultiplier = 0.5f; 
    private float maxZoomMultiplier = 1.1f;
    private Vector2 margin = new Vector2(300, 300);

    //Makes the camera push further ahead of moving players
    private float lookAheadFactor = 1f;
    private float maxLookAheadDistance = 800f;
    
    //How fast the camera reacts to sudden changes in direction (higher = snappier)
    private float velocitySmoothSpeed = 5f;

    //Keeps the zoom from violently snapping when velocity spikes
    private float velocityZoomInfluence = 0.5f; 

    private Vector2 smoothedVelocity = Vector2.Zero; 

    private Vector2 targetPosition;
    private Vector2 targetZoom;
    private Level currentLevel;

    public override void _Ready(){
        LimitLeft = -10000000;
        LimitTop = -10000000;
        LimitRight = 10000000;
        LimitBottom = 10000000;
    }

    public override void _Process(double delta){
        if(Game.CurrentScene == Game.SceneType.Game){
            CameraMovements((float)delta);
        }else if(Game.CurrentScene == Game.SceneType.Menu){
            MenuScene.SetCamera();
            GlobalPosition = Vector2.Zero;
        }else{
            GlobalPosition = Vector2.Zero;
        }
    }

    private void CameraMovements(float fDelta){
        if(currentLevel != Level.LevelNode){
            currentLevel = Level.LevelNode;
            
            CollisionShape2D initBounds = Level.LevelNode.GetNodeOrNull<CollisionShape2D>("CameraBoundary");
            if(initBounds == null) return;
            GlobalPosition = initBounds.GlobalPosition;
            
            targetPosition = GlobalPosition;
            Zoom = Game.ContentScaleVector2 * Level.LevelNode.CameraZoom;
            targetZoom = Zoom;
            smoothedVelocity = Vector2.Zero; 
        }
        
        //Grab visual rectangle limits and zoom floor
        CollisionShape2D bounds = Level.LevelNode.GetNodeOrNull<CollisionShape2D>("CameraBoundary");
        if(bounds == null) return;
        RectangleShape2D rectShape = (RectangleShape2D)bounds.Shape;
        
        Vector2 trueCenter = bounds.GlobalPosition;
        Vector2 mapExtents = rectShape.Size / 2f;

        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 mapSize = mapExtents * 2f;
        
        float minAllowedZoomX = viewportSize.X / mapSize.X;
        float minAllowedZoomY = viewportSize.Y / mapSize.Y;
        float absoluteMinZoomFloor = Mathf.Max(minAllowedZoomX, minAllowedZoomY);

        //Gather target data
        Vector2 minPos = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxPos = new Vector2(float.MinValue, float.MinValue);
        Vector2 totalRawVelocity = Vector2.Zero;
        
        int activeTargets = 0;
        int velocityContributors = 0;

        //Process Players
        if(Game.Players != null){
            foreach(Player player in Game.Players){
                if(player == null || !IsInstanceValid(player)) continue; 
                if(Level.IsPositionOffscreenOrDead(player.Rb.GlobalPosition)) continue; 
                
                Vector2 pos = player.Rb.GlobalPosition;
                minPos.X = Mathf.Min(minPos.X, pos.X);
                minPos.Y = Mathf.Min(minPos.Y, pos.Y);
                maxPos.X = Mathf.Max(maxPos.X, pos.X);
                maxPos.Y = Mathf.Max(maxPos.Y, pos.Y);
                
                totalRawVelocity += player.Rb.LinearVelocity;
                activeTargets++;
                velocityContributors++;
            }
        }

        //Process Secondary Targets
        if(Mode.GetCameraTargets() != null){
            foreach(Node2D targetNode in Mode.GetCameraTargets()){
                if(targetNode == null || !IsInstanceValid(targetNode)) continue;
                if(Level.IsPositionOffscreenOrDead(targetNode.GlobalPosition)) continue;
                
                Vector2 pos = targetNode.GlobalPosition;
                minPos.X = Mathf.Min(minPos.X, pos.X);
                minPos.Y = Mathf.Min(minPos.Y, pos.Y);
                maxPos.X = Mathf.Max(maxPos.X, pos.X);
                maxPos.Y = Mathf.Max(maxPos.Y, pos.Y);
                
                activeTargets++;
                
                if (targetNode is RigidBody2D rb) {
                    totalRawVelocity += rb.LinearVelocity;
                    velocityContributors++;
                }
            }
        }

        //Handle targets and zoom
        if(activeTargets == 0 || Mode.Finished){
            targetPosition = trueCenter;
            targetZoom = new Vector2(absoluteMinZoomFloor, absoluteMinZoomFloor);
            smoothedVelocity = Vector2.Zero;
        }else{
            //Calculate look ahead with velocity smoothing
            Vector2 rawAverageVelocity = velocityContributors > 0 ? (totalRawVelocity / velocityContributors) : Vector2.Zero;
            smoothedVelocity = smoothedVelocity.Lerp(rawAverageVelocity, velocitySmoothSpeed * fDelta);
            Vector2 lookAheadOffset = smoothedVelocity * lookAheadFactor;

            if(lookAheadOffset.Length() > maxLookAheadDistance){
                lookAheadOffset = lookAheadOffset.Normalized() * maxLookAheadDistance;
            }

            targetPosition = ((minPos + maxPos) / 2f) + lookAheadOffset;

            //Padding scales gently so the camera doesn't violently zoom out when moving fast
            Vector2 lookAheadPadding = new Vector2(Mathf.Abs(lookAheadOffset.X), Mathf.Abs(lookAheadOffset.Y)) * velocityZoomInfluence;
            Vector2 requiredSize = maxPos - minPos + margin + lookAheadPadding;

            //Calculate zoom
            Vector2 baseZoom = Game.ContentScaleVector2 * Level.LevelNode.CameraZoom;
            
            float screenSafeZone = 0.8f; 
            Vector2 usableWorldSize = (viewportSize / baseZoom) * screenSafeZone;

            float zoomX = usableWorldSize.X / requiredSize.X;
            float zoomY = usableWorldSize.Y / requiredSize.Y;
            
            float calculatedZoom = Mathf.Min(zoomX, zoomY);
            float relativeZoom = Mathf.Clamp(calculatedZoom, minZoomMultiplier, maxZoomMultiplier);
            Vector2 desiredZoom = baseZoom * relativeZoom;

            targetZoom.X = Mathf.Max(desiredZoom.X, absoluteMinZoomFloor);
            targetZoom.Y = Mathf.Max(desiredZoom.Y, absoluteMinZoomFloor);
        }

        //Limits
        Vector2 currentLensExtents = (viewportSize / targetZoom) / 2f;

        float minCamX = trueCenter.X - mapExtents.X + currentLensExtents.X;
        float maxCamX = trueCenter.X + mapExtents.X - currentLensExtents.X;
        float minCamY = trueCenter.Y - mapExtents.Y + currentLensExtents.Y;
        float maxCamY = trueCenter.Y + mapExtents.Y - currentLensExtents.Y;

        if(minCamX > maxCamX) targetPosition.X = trueCenter.X;
        else targetPosition.X = Mathf.Clamp(targetPosition.X, minCamX, maxCamX);

        if(minCamY > maxCamY) targetPosition.Y = trueCenter.Y;
        else targetPosition.Y = Mathf.Clamp(targetPosition.Y, minCamY, maxCamY);

        //Apply zoom lerp
        GlobalPosition = GlobalPosition.Lerp(targetPosition, moveSpeed * fDelta);
        float currentZoomSpeed = (targetZoom.X < Zoom.X) ? zoomOutSpeed : zoomInSpeed;
        Zoom = Zoom.Lerp(targetZoom, currentZoomSpeed * fDelta);
    }
}