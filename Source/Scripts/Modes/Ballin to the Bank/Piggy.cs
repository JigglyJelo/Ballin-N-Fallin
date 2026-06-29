using Godot;
using System;
using System.Collections.Generic;

public partial class Piggy : Node2D{
	private Sprite2D sprite,wing;
	private Area2D area;
	private Label moneyText;
	private Node2D visualsNode;
	private Dictionary<int,float> collidingPlayerTimers = new Dictionary<int, float>();
	private readonly float DEPOSIT_TIME = 8f/BTTB.MoneyToWin;
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
					int heldMoney = BTTB.HeldPlayerMoney[i];
					if(heldMoney > 0){
						//How much faster you deposit if you hold 100% of the win amount.
						const float MAX_DEPOSIT_SPEED = 1;
						int playerId = i + 1;
						
						float percentOfWinCondition = (float)heldMoney / BTTB.MoneyToWin;
						float speedMultiplier = 1 + (percentOfWinCondition * MAX_DEPOSIT_SPEED);

						float currentDepositTime = DEPOSIT_TIME / speedMultiplier;

						collidingPlayerTimers[playerId] += fDelta;

						if(collidingPlayerTimers[playerId] >= currentDepositTime){
							int coinsToDeposit = (int)(collidingPlayerTimers[playerId] / currentDepositTime);
							if(coinsToDeposit > heldMoney) coinsToDeposit = heldMoney;
							if(coinsToDeposit > 255) coinsToDeposit = 255;

							if(!Mode.Finished && coinsToDeposit > 0){
								Rpc(nameof(PlayerDeposited), (byte)i, (byte)coinsToDeposit);
							}

							collidingPlayerTimers[playerId] -= coinsToDeposit * currentDepositTime;
						}
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
					if(!Mode.Finished) Rpc(nameof(PlayerDeposited),(byte)(player.Index), (byte)1);
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
		moneyText.Text = "$"+BTTB.DepositedMoney[player.Index] + " / $" + BTTB.MoneyToWin;
		if(Online.IsHost() && BTTB.DepositedMoney[player.Index] == BTTB.MoneyToWin && !Mode.Finished) Mode.GameFinished();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void PlayerDeposited(byte playerIndex, byte depositeAmount){
		if(!Mode.Finished){
			Player player = Game.Players[playerIndex];
			if(BTTB.HeldPlayerMoney[player.Index] >= depositeAmount){
				BTTB.HeldPlayerMoney[player.Index] -= depositeAmount;
				BTTB.DepositedMoney[player.Index] += depositeAmount;
				player.Visuals.ShowPlayerText();
				UpdateMoneyText(Game.Players[playerIndex]);
				if(MusicPlayer.GetPitch() != BTTB.FAST_MUSIC_SPEED && BTTB.DepositedMoney[player.Index] > BTTB.MoneyToWin*0.75f){
					MusicPlayer.SetPitch(BTTB.FAST_MUSIC_SPEED);
				}
			}
		}
	}
}