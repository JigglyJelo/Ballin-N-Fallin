using Godot;

public class StopSign : SingleUseItem{
	public StopSign(Player player,byte amount): base(player,ItemEnum.StopSign,amount){}

	public override void ItemAbility(){
		Player.Rb.LinearVelocity = Vector2.Zero;
		Player.CanLaunch = true;
        Player.CanSlam = true;
        Player.Rb.Freeze = true;
		Player.FrozenTimer = 0;
	}
}