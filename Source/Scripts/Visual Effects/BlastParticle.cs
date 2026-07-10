using Godot;
using System;

public partial class BlastParticle : CpuParticles2D{
	private Vector2 deathPosition;
	
	private const float PADDING = 50f;

	public void Initialize(Vector2 deathPos){
		deathPosition = deathPos;
		UpdateEdgePosition();
	}

	public override void _Process(double delta){
		UpdateEdgePosition();
	}

	private void UpdateEdgePosition(){
		
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 visibleSize = viewportSize / DynamicCamera.CameraNode.Zoom;
		Vector2 extents = visibleSize / 2f;
		
		float angle = DynamicCamera.CameraNode.GlobalPosition.AngleToPoint(deathPosition);
		float cos = Mathf.Cos(angle);
		float sin = Mathf.Sin(angle);
		
		float tx = Mathf.Abs(cos) > Mathf.Epsilon ? Mathf.Abs(extents.X / cos) : float.MaxValue;
		float ty = Mathf.Abs(sin) > Mathf.Epsilon ? Mathf.Abs(extents.Y / sin) : float.MaxValue;
		float t = Mathf.Min(tx, ty);
		
		Vector2 offset = new Vector2(cos * t, sin * t);
		GlobalPosition = DynamicCamera.CameraNode.GlobalPosition + offset;
		
		if(t == tx){
			if(cos > 0){
				Rotation = MathF.PI;
				GlobalPosition += new Vector2(PADDING, 0);
			}else{
				Rotation = 0;
				GlobalPosition -= new Vector2(PADDING, 0);
			}
		}else{
			if(sin > 0){
				Rotation = (3f * MathF.PI) / 2f;
				GlobalPosition += new Vector2(0, PADDING);
			}else{
				Rotation = MathF.PI / 2f;
				GlobalPosition -= new Vector2(0, PADDING);
			}
		}
	}
}