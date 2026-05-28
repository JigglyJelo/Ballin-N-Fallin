using Godot;
using System.Text;
using System.Collections.Generic;

public partial class ScoreScreen : CanvasLayer{
	//Score screen variables
	public static Vector2 BackgroundStartingPosition;
	private readonly PackedScene endScreenScene = GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Score Screen/PlayerEndscreenResult.tscn");
	private EndscreenResult[] playerResults = new EndscreenResult[Game.TotalPlayers];
	private int playerScoresDone = 0;
	private Label topText,winnerText;
	//Next round variables
	private Mode.GameMode nextMode;
	public static bool TourFinished = false;
	private CanvasLayer backgroundLayer;
	private AudioStreamPlayer music;
	public override void _Ready(){
		TourFinished = false;
		Game.Camera.Zoom = Vector2.One;
		for(int i = 0; i < Game.MAX_PLAYERS; i++) Input.StopJoyVibration(i);
		music = GetParent().GetNode<AudioStreamPlayer>("AudioStreamPlayer");
		backgroundLayer = Game.GameNode.GetNodeOrNull<CanvasLayer>("BackgroundLayer");
		topText = GetNode<Label>("ScoreText");
		winnerText = GetNode<Label>("WinnerText");
		Multiplayer.PeerDisconnected += PlayerDisconnected;
		Scale = Game.ContentScaleVector2;
		if(Online.IsHost() && Tour.IsTour){
			nextMode = Tour.ChooseNextMode();
			Game.GetRandomLevel(nextMode);
		}
		topText.Text = Tour.IsTour ? Tour.TotalScore + " Points to Win!" : "Results";

		bool playVictoryMusic = false;
		for(int i = 0; i < Game.TotalPlayers; i++){
			if(Tour.PlayerScores[i] >= Tour.TotalScore){
				playVictoryMusic = true;
				TourFinished = true;
				break;
			}
		}

		if(!string.IsNullOrEmpty(Game.CustomSoundtrack)){
			AudioStream customSong = MusicPlayer.GetCustomSong(playVictoryMusic ? "Victory" : "Score");
			if(customSong != null) music.Stream = customSong;
			else if(playVictoryMusic) music.Stream = GD.Load<AudioStream>("res://Assets/Music/Victory.ogg");
		}else if(playVictoryMusic){
			music.Stream = GD.Load<AudioStream>("res://Assets/Music/Victory.ogg");
		}

		music.Play();

		if(playVictoryMusic) GD.Print("VICTORY");
		for(int i = 1; i <= Game.TotalPlayers; i++){
			CreateNextEndScreen(i);
		}
		PositionPlayerResults();
		if(backgroundLayer != null){
			backgroundLayer.Layer = -1;
			backgroundLayer.Scale = Game.ContentScaleVector2;
		}
	}

    public async void EndScreenDone(){
		if(++playerScoresDone >= Game.TotalPlayers){
			//Check if done and gather winners
			List<int> winners = new List<int>();
			//int topScore = Tour.PlayerScores.Max();
			int topScore = Tour.PlayerScores[0];
        	for(int i = 1; i < Tour.PlayerScores.Length; i++){
            	if(Tour.PlayerScores[i] > topScore) topScore = Tour.PlayerScores[i];
        	}
			for(int i = 0; i < Game.TotalPlayers; i++){
				if(Tour.PlayerScores[i] >= Tour.TotalScore && Tour.PlayerScores[i] == topScore){
					TourFinished = true;
					winners.Add(i + 1);
				}
			}

			//Create winning text string and give crown
			if(winners.Count > 0 && Tour.IsTour){
				if(winners.Count == 1){
					winnerText.Text = Game.GetUsername(winners[0]) + " is Ballin!";
					int winnerIndex = winners[0]-1;
					//winnerText.SelfModulate = Online.IsOnline ? Online.PlayerColors[winnerIndex] : Game.PlayerColors[Game.InputIds[winnerIndex]-1];
					winnerText.SelfModulate = Game.PlayerDatas[winnerIndex].PlayerColor;
					if(Online.IsOnline && Online.Network == Online.NetworkType.Steam){
						Node node = GetNode("WinnerText/ProfilePicture");
						//node.Call("set_profile_picture",Steam.UUIDToSteamId[Online.PlayerInfos[winnerIndex].UUID],400);
					}
					//Add crown
					ScoreScreenPlayer player = playerResults[winnerIndex].ScorePlayer;
					player.AddChild(CTK.GetCrownSprite(player.EyeSprite.FlipH));
					player.EyeSprite.Texture = PlayerVisuals.GetEyeTexture(Player.Emotion.Happy,false);
				}else{
					StringBuilder winTextBuilder = new StringBuilder();
					winTextBuilder.Append(Game.GetUsername(winners[0]));
					//Add crown
					ScoreScreenPlayer player = playerResults[winners[0]-1].ScorePlayer;
					player.AddChild(CTK.GetCrownSprite(player.EyeSprite.FlipH));
					for(int i = 1; i < winners.Count; i++){
						if(winners.IndexOf(winners[i]) != winners.Count - 1) winTextBuilder.Append(" ,");
						else winTextBuilder.Append(" & ");
						winTextBuilder.Append(Game.GetUsername(winners[i]));
						//Add crown
						player = playerResults[winners[i]-1].ScorePlayer;
						player.AddChild(CTK.GetCrownSprite(player.EyeSprite.FlipH));
						player.EyeSprite.Texture = PlayerVisuals.GetEyeTexture(Player.Emotion.Happy,false);
					}
					winTextBuilder.Append(" are Ballin!");
					winnerText.Text = winTextBuilder.ToString();
				}
				TweenWinnerText();
			}
			const double WAIT_TIME = 3.275+0.3;
			if(Online.IsHost()){
				if(!Tour.IsTour){
					await ToSignal(GetTree().CreateTimer(1,false), "timeout");
					SceneTransitioner.RpcReturnToLobby();
				}else if(TourFinished){
					await ToSignal(GetTree().CreateTimer(WAIT_TIME,false), "timeout");
					if(Game.AutoStartNewTour){
						SceneTransitioner.RpcStartNewTourFromScoreScreen();
						//Tour.PrepareTour();
						//SceneTransitioner.RpcStartNextRound((byte)Game.CurrentMode,Game.CurrentLevelName,Game.CurrentFolderPath);
					}else{
						AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Menus/ScoreScreenMenu.tscn").Instantiate<ScoreScreenMenu>());
					}
				}else{
					SceneTransitioner.RpcStartNextRound((byte)nextMode,Game.CurrentLevelName,Game.CurrentFolderPath);
				}
			}else{
				if(TourFinished && !Game.AutoStartNewTour){
					await ToSignal(GetTree().CreateTimer(WAIT_TIME,false), "timeout");
					GetNode<Label>("WaitingText").Visible = true;
				}
			}
		}
	}

	private void TweenWinnerText(){
		topText.Visible = false;
		winnerText.Scale = Vector2.Zero;
		winnerText.Visible = true;
		Tween winnerTextTween = GetParent().CreateTween();
		winnerTextTween.TweenProperty(winnerText,"scale",new Vector2(1.5f,1.5f),0.25);
	}

	private void CreateNextEndScreen(int id){
		EndscreenResult endScreen = endScreenScene.Instantiate<EndscreenResult>();
		endScreen.Id = id;
		playerResults[id-1] = endScreen;
		AddChild(endScreen);
	}

	private void PositionPlayerResults(){
    	const int MIDPOINT = 1728;
    	const int SPACING = 400;
		const int Y_POS = 1900;

    	switch(Game.TotalPlayers){
		    case 1:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT, Y_POS));
		        break;
		    case 2:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - SPACING, Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT + SPACING, Y_POS));
		        break;
		    case 3:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - (2 * SPACING), Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT, Y_POS)); // Centered
		        playerResults[2].SetDeferred("position", new Vector2(MIDPOINT + (2 * SPACING), Y_POS));
		        break;
		    case 4:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - (3 * SPACING), Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT - SPACING, Y_POS));
		        playerResults[2].SetDeferred("position", new Vector2(MIDPOINT + SPACING, Y_POS));
		        playerResults[3].SetDeferred("position", new Vector2(MIDPOINT + (3 * SPACING), Y_POS));
		        break;
		    case 5:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - (4 * SPACING), Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT - (2 * SPACING), Y_POS));
		        playerResults[2].SetDeferred("position", new Vector2(MIDPOINT, Y_POS)); // Centered
		        playerResults[3].SetDeferred("position", new Vector2(MIDPOINT + (2 * SPACING), Y_POS));
		        playerResults[4].SetDeferred("position", new Vector2(MIDPOINT + (4 * SPACING), Y_POS));
		        break;
		    case 6:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - (5 * SPACING), Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT - (3 * SPACING), Y_POS));
		        playerResults[2].SetDeferred("position", new Vector2(MIDPOINT - SPACING, Y_POS));
		        playerResults[3].SetDeferred("position", new Vector2(MIDPOINT + SPACING, Y_POS));
		        playerResults[4].SetDeferred("position", new Vector2(MIDPOINT + (3 * SPACING), Y_POS));
		        playerResults[5].SetDeferred("position", new Vector2(MIDPOINT + (5 * SPACING), Y_POS));
				Scale = Game.ContentScaleVector2 * 0.875f;
				Offset = new Vector2(240, 200) * (Game.Resolution / 2160f);
				topText.GlobalPosition -= new Vector2(0,155);
		        break;
		    case 7:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - (6 * SPACING), Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT - (4 * SPACING), Y_POS));
		        playerResults[2].SetDeferred("position", new Vector2(MIDPOINT - (2 * SPACING), Y_POS));
		        playerResults[3].SetDeferred("position", new Vector2(MIDPOINT, Y_POS)); // Centered
		        playerResults[4].SetDeferred("position", new Vector2(MIDPOINT + (2 * SPACING), Y_POS));
		        playerResults[5].SetDeferred("position", new Vector2(MIDPOINT + (4 * SPACING), Y_POS));
		        playerResults[6].SetDeferred("position", new Vector2(MIDPOINT + (6 * SPACING), Y_POS));
				Scale = Game.ContentScaleVector2 * (2/3f);
				Offset = new Vector2(605, 520)  * (Game.Resolution / 2160f);
				topText.GlobalPosition -= new Vector2(0,260-155);
		        break;
		    case 8:
		        playerResults[0].SetDeferred("position", new Vector2(MIDPOINT - (7 * SPACING), Y_POS));
		        playerResults[1].SetDeferred("position", new Vector2(MIDPOINT - (5 * SPACING), Y_POS));
		        playerResults[2].SetDeferred("position", new Vector2(MIDPOINT - (3 * SPACING), Y_POS));
		        playerResults[3].SetDeferred("position", new Vector2(MIDPOINT - SPACING, Y_POS));
		        playerResults[4].SetDeferred("position", new Vector2(MIDPOINT + SPACING, Y_POS));
		        playerResults[5].SetDeferred("position", new Vector2(MIDPOINT + (3 * SPACING), Y_POS));
		        playerResults[6].SetDeferred("position", new Vector2(MIDPOINT + (5 * SPACING), Y_POS));
		        playerResults[7].SetDeferred("position", new Vector2(MIDPOINT + (7 * SPACING), Y_POS));
		        break;
		}
	}

	private void PlayerDisconnected(long id){  //Probably can delete this but testing it would be annoying so its here still for now
		foreach(PlayerData playerInfo in Game.PlayerDatas){
			if(playerInfo.UUID == id){
				if(!Game.DisconnectedDatas.Contains(playerInfo))
					Game.DisconnectedDatas.Add(playerInfo);
				break;
			}
		}
	}

	public void MenuBackgroundFadeout(){
		MenuBackground background = backgroundLayer.GetNode<MenuBackground>("Background");
		background.GlobalPosition = new Vector2(1920,1080);
		backgroundLayer.Layer = 1;
		backgroundLayer.FollowViewportEnabled = false;
		backgroundLayer.Reparent(Game.GameNode);
	}
}