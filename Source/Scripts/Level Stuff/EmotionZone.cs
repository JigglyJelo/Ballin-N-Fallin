using Godot;
using System;
using System.Collections.Generic;

public partial class EmotionZone : Area2D{
	[Export]
	public Player.Emotion Emotion = Player.Emotion.Neutral;
	private List<Player> players = new List<Player>();

	public void _on_body_entered(PhysicsBody2D body){
		if(body.GetParent().IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			player.PlayerEmotion = Emotion;
			players.Add(player);
		}
	}

	public override void _PhysicsProcess(double delta){
		foreach(Player player in players) player.PlayerEmotion = Emotion;
	}

	public void _on_body_exited(PhysicsBody2D body){
		if(body.GetParent().IsInGroup("Player")){
			Player player = body.GetParent() as Player;
			players.Remove(player);
		}
	}
}