using System;
using Godot;

public class BigFungus : TransformItem{
	public BigFungus(Player player): base(player,ItemEnum.BigFungus,8){
		if(Game.CurrentMode == Mode.GameMode.Deathmatch || Game.CurrentMode == Mode.GameMode.Survival) transformTime = 5;
	}

	public override void UseItem(){
		if(!Activated) Player.PlayerEmotion = Player.Emotion.Angry;
		Activated = true;
		SetTransformation();
	}

    public override void SetTransformation(){
		if(MathF.Abs(Player.PlayerScale - 2f) > 0.0001f){
			Player.PlayerScale = 2f;
			PhysicsMaterial physicsMaterial = new PhysicsMaterial();
       		physicsMaterial.Bounce = 0.5f;
			Player.Rb.PhysicsMaterialOverride = physicsMaterial;
		}
		else Player.ResetTransformation();
    }
}