using Godot;

public class BowlingBall : TransformItem{
	public BowlingBall(Player player): base(player,ItemEnum.BowlingBall,8){}

	public override void UseItem(){
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
		if(Player.Rb.GravityScale == Player.GRAVITY && Player.Rb.PhysicsMaterialOverride.Bounce == Player.BOUNCE){
			Player.Rb.GravityScale = 3;
			PhysicsMaterial physicsMaterial = new PhysicsMaterial();
			physicsMaterial.Bounce = 0.1f;
			physicsMaterial.Friction = 1;
			Player.Rb.PhysicsMaterialOverride = physicsMaterial;
		}else Player.ResetTransformation();   
    }
}