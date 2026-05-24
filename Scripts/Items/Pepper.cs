using Godot;
using System;
public class Pepper : SingleUseItem{
	public Pepper(Player player,byte amount): base(player,ItemEnum.Pepper,amount){}

	public override void ItemAbility(){
		Player.PlayerEmotion = Player.Emotion.Angry;
		Player.Rb.Freeze = false;
		//If player is moving in direction opposite of aiming stop their momentum in that direction
		if(Math.Sign(Player.Rb.LinearVelocity.X) != Math.Sign(Player.InputVector.X)) 
			Player.Rb.LinearVelocity = new Vector2(0,Player.Rb.LinearVelocity.Y);
		if(Math.Sign(Player.Rb.LinearVelocity.Y) != Math.Sign(Player.InputVector.Y)) 
			Player.Rb.LinearVelocity = new Vector2(Player.Rb.LinearVelocity.X,0);
		//Launch
		Player.Rb.SetDeferred("linear_velocity",Player.Rb.LinearVelocity + (Player.InputVector * (Player.MAX_LAUNCH_POWER+Player.MIN_LAUNCH_POWER) * 1.5f));//(Player.SPEED_CAP / (3+(1f/3))))
	}
}