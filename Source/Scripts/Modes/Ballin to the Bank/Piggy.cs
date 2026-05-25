using Godot;
using System;
using System.Collections.Generic;

public partial class Piggy : Node2D{
	private Sprite2D sprite,wing;
	private Area2D area;
	private Label moneyText;
	private Node2D visualsNode;
	private Dictionary<int,float> collidingPlayerTimers = new Dictionary<int, float>();
	private float depositTime;
	private float flapTime = 0;
	private const float FLAP_SPEED = 2;
	private const float FLAP_DISTANCE = 50;
	private bool moveToNewPosition = false;
	private Vector2 newPosition = new Vector2(1000,0);

	public override void _Ready(){
		area = GetNode<Area2D>("Area2D");
		visualsNode = GetNode<Node2D>("Visuals");
		sprite = GetNode<Sprite2D>("Visuals/Pig");
		wing = GetNode<Sprite2D>("Visuals/Pig/Wing");
		moneyText = GetNode<Label>("Visuals/Label");
		depositTime = 8f/BTTB.MoneyToWin;
		Mode.AddCameraTarget(area);
	}

	public override void _Process(double delta){
		float fDelta = (float)delta;
		flapTime += fDelta*FLAP_SPEED; // Accumulate time each frame
    	sprite.Position = new Vector2(sprite.Position.X, MathF.Sin(flapTime) * FLAP_DISTANCE);
		if(moveToNewPosition) visualsNode.GlobalPosition = area.GlobalPosition.Lerp(newPosition,fDelta);
		wing.Rotation = MathF.Sin(flapTime) * 0.4f + 0.25f;
	}

    public override void _PhysicsProcess(double delta){
		float fDelta = (float)delta;
		if(Online.IsHost()){
			for(int i = 0; i < Game.TotalPlayers; i++){
				if(collidingPlayerTimers.ContainsKey(i+1)){
					collidingPlayerTimers[i+1] += fDelta;
					if(collidingPlayerTimers[i+1] >= depositTime){
						if(!Mode.Finished) Rpc(nameof(PlayerDeposited),(byte)i);
						collidingPlayerTimers[i+1] = 0;
					}
				}
			}
		}
		
		if(moveToNewPosition){
			area.GlobalPosition = area.GlobalPosition.Lerp(newPosition,fDelta);
			if(area.GlobalPosition.IsEqualApprox(newPosition)){
				moveToNewPosition = false;
				visualsNode.GlobalPosition = newPosition;
			}
		} 
    }

    public void _on_area_2d_body_entered(PhysicsBody2D body){
		if(Online.IsHost()){
			if(body.IsInGroup("Player")){
				Player player = body.GetParent() as Player;
				if(!collidingPlayerTimers.ContainsKey(player.Id)){
					collidingPlayerTimers.Add(player.Id,0);
					if(!Mode.Finished) Rpc(nameof(PlayerDeposited),player.Id-1);
				}
			}
		}
	}

	public void _on_area_2d_body_exited(PhysicsBody2D body){
		if(Online.IsHost()){
			if(body.IsInGroup("Player")){
				Player player = body.GetParent() as Player;
				if(collidingPlayerTimers.ContainsKey(player.Id)){
					collidingPlayerTimers.Remove(player.Id);
				}
			}
		}
	}

	private void UpdateMoneyText(Player player){
		moneyText.SelfModulate = player.PlayerColor;
		moneyText.Text = "$"+BTTB.DepositedMoney[player.Id-1] + " / $" + BTTB.MoneyToWin;
		if(Online.IsHost() && BTTB.DepositedMoney[player.Id-1] == BTTB.MoneyToWin && !Mode.Finished) Mode.GameFinished();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void PlayerDeposited(byte playerIndex){
		if(!Mode.Finished){
			Player player = Game.Players[playerIndex];
			if(BTTB.HeldPlayerMoney[player.Id-1] >= 1){
				BTTB.HeldPlayerMoney[player.Id-1]--;
				BTTB.DepositedMoney[player.Id-1]++;
				player.ShowPlayerText();
				UpdateMoneyText(Game.Players[playerIndex]);
				if(MusicPlayer.GetPitch() != BTTB.FAST_MUSIC_SPEED && BTTB.DepositedMoney[player.Id-1] > BTTB.MoneyToWin*0.75f){
					MusicPlayer.SetPitch(BTTB.FAST_MUSIC_SPEED);
				}
			}
		}
	}
}