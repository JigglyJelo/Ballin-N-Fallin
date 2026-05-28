using Godot;

public class Booll : TransformItem{
	public Booll(Player player): base(player,ItemEnum.Booll,10){
		if(Game.CurrentMode == Mode.GameMode.CrownTheKing || Game.CurrentMode == Mode.GameMode.HotPotato) transformTime = 4;
	}

	public override void UseItem(){
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
		if(Player.Visuals.BallSprite.SelfModulate.A == 1 && !Player.Invulnerable){
			foreach(Player player in Game.Players) Player.Rb.AddCollisionExceptionWith(player.Rb);
			Player.Visuals.BallSprite.SelfModulate = new Color(Player.Visuals.BallSprite.SelfModulate,0.5f);
			Player.Visuals.ShadingSprite.SelfModulate = new Color(Player.Visuals.ShadingSprite.SelfModulate,0);
			Player.ZIndex = Player.ZIndex;
		}else if(!Player.Invulnerable){
			Player.ResetTransformation();
		}
    }
}