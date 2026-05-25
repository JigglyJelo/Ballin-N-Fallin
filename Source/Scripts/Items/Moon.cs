public class Moon : TransformItem{
	public Moon(Player player): base(player,ItemEnum.Moon,10){}

	public override void UseItem(){
		if(!Activated) Player.PlayerEmotion = Player.Emotion.Shocked;
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
		if(Player.Rb.GravityScale == Player.GRAVITY) Player.Rb.GravityScale = 0;
		else Player.Rb.GravityScale = Player.GRAVITY;
    }
}