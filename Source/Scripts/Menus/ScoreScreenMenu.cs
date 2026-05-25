using Godot;

public partial class ScoreScreenMenu : VerticalMenu{
	private Label playText,changeText,quitText;
	public override void _Ready(){
		totalSelections = 3;
		//Selection = 1;
		playText = GetNode<Label>("Selections/PlayText");
		changeText = GetNode<Label>("Selections/ChangeText");
		quitText = GetNode<Label>("Selections/QuitText");
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		if(Online.IsHost()){
			InputChecks(delta,(int)Game.PlayerDatas[0].InputDevice);
		}
	}

	protected override void MenuChoose(int choice){
		switch(choice){
			case 1:
				SFX.Play("Confirm");
				//Tour.PrepareTour();
				//SceneTransitioner.RpcStartNextRound((byte)Game.CurrentMode,Game.CurrentLevelName,Game.CurrentFolderPath);
				SceneTransitioner.RpcStartNewTourFromScoreScreen();
				break;
			case 2:
				SceneTransitioner.RpcReturnToLobby();
				break;
			case 3:
				CanvasLayer backgroundLayer = Game.GameNode.GetNodeOrNull<CanvasLayer>("BackgroundLayer");
				if(backgroundLayer != null){
					MenuBackground.StartPoint = backgroundLayer.GetNode<Polygon2D>("Background").TextureOffset;
					backgroundLayer.QueueFree();
				}
				if(Online.IsOnline) Online.Disconnect("Left Lobby");
				else SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
				break;
		}
	}
	public override void MenuBack(){}
}