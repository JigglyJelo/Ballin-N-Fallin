using System;

public class SmallBall : TransformItem{
	public SmallBall(Player player): base(player,ItemEnum.SmallBall,10){}

	public override void UseItem(){
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
		if(MathF.Abs(Player.PlayerScale - 0.5f) > 0.0001f) Player.PlayerScale = 0.5f;
		else Player.ResetTransformation();
    }
}