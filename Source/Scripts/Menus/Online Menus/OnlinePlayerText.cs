using System;
using Godot;

public partial class OnlinePlayerText : Node2D{
	public int UUID;
	public int Id;
	public Label PingText;
	private float pingTimer;
	public override void _Ready(){
		ResetPlayerText();
		PingText = GetNode<Label>("PingText");
	}

    public override void _PhysicsProcess(double delta){
		if(Visible && Online.PeerIsActive()){
			pingTimer += (float)delta;
			if(pingTimer >= 0.1){
				pingTimer = 0;
				if(UUID == Game.GameNode.Multiplayer.GetUniqueId()){
					PingText.Text = PingGetter.LastPing.ToString();
					PingText.SelfModulate = OnlineLobby.GetPingTextColor(PingGetter.LastPing);
				}else{
					PingText.Text = PingGetter.Pings[Id].ToString();
					PingText.SelfModulate = OnlineLobby.GetPingTextColor(PingGetter.Pings[Id]);
				}
			}
		}
    }

	public void ResetPlayerText(){
		Label usernameText = GetNode<Label>("UsernameText");
		for(int i = 0; i < Game.PlayerDatas.Count; i++){
			if(Game.PlayerDatas[i].UUID == UUID){
				usernameText.SelfModulate = Game.PlayerDatas[i].PlayerColor;
				usernameText.Text = (UUID == Multiplayer.GetUniqueId()) ? Online.Username : Game.PlayerDatas[i].Username;
				Id = i;
				Visible = true;
				return;
			}
		}
		Visible = false;
		GetNode<Sprite2D>("Icon").Texture = null;
		UUID = 0;
	}
}