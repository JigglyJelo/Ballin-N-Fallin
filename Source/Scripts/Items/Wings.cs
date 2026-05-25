public class Wings : TransformItem{
	public Wings(Player player): base(player,ItemEnum.Wings,8){}

	public override void UseItem(){
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
        //Wing visual
    }
}