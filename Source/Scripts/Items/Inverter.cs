public class Inverter : TransformItem{
	public Inverter(Player player): base(player,ItemEnum.Inverter,10){}

	public override void UseItem(){
		if(!Activated) Player.PlayerEmotion = Player.Emotion.Shocked;
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
		Player.Rb.Sleeping = false;
        Player.Rb.GravityScale *= -1;
		Player.FlipV(!Player.LinesSprite.FlipV);
    }
}