using Godot;

public partial class InstructionsText : Node2D{
	private float lifetime;
	private const float SECONDS_PER_CHARACTER = 1f/15;
	private float timer;
	private Label modeText,instructionsText;
	private CanvasGroup modeGroup,instructionsGroup;
	public async override void _Ready(){
		GetNode<CanvasLayer>("CanvasLayer").Scale =  Game.ContentScaleVector2;
		modeText = GetNode<Label>("CanvasLayer/ModeGroup/ModeText");
		modeGroup = GetNode<CanvasGroup>("CanvasLayer/ModeGroup");
		if(Game.CurrentMode != Mode.GameMode.Survival){
			modeText.Text = Mode.EnumToString(Game.CurrentMode);
		}else{
			if(Game.CurrentFolderPath.Contains("Trash Compactor")){
				modeText.Text = "Trash Compactor";
			}else if(Game.CurrentFolderPath.Contains("Sumo")){
				modeText.Text = "Sumo";
			}else{
				modeText.Text = Mode.EnumToString(Game.CurrentMode);
			}
		}
		
		instructionsText = GetNode<Label>("CanvasLayer/InstructionGroup/InstructionText");
		instructionsGroup = GetNode<CanvasGroup>("CanvasLayer/InstructionGroup");
		instructionsText.Text = Mode.ModeNode.Instructions;
		float timeScaleMultiplier = Game.CurrentMode == Mode.GameMode.Race ? Countdown.RACE_START_TIMESCALE : 1;
		modeText.Scale = Vector2.Zero;
		const double MODE_TEXT_GROW_DELAY = 0.2;
		await ToSignal(GetTree().CreateTimer(MODE_TEXT_GROW_DELAY,false,false,true), "timeout");
		const double MODE_TEXT_GROW_SPEED = 0.2;
		Tween modeTween = CreateTween();
		modeTween.TweenProperty(modeText,"scale",new Vector2(2,2),MODE_TEXT_GROW_SPEED*timeScaleMultiplier);
		modeTween.TweenCallback(Callable.From(FadeoutModeText));
		instructionsGroup.SelfModulate = Game.CLEAR;
		const double INSTRUCTION_FADE_IN_DELAY = 0.5;
		await ToSignal(GetTree().CreateTimer(MODE_TEXT_GROW_SPEED+INSTRUCTION_FADE_IN_DELAY,false,false,true), "timeout");
		const double INSTRUCTION_FADE_SPEED = 0.25;
		Tween instructionsTween = CreateTween();
		instructionsTween.TweenProperty(instructionsGroup,"self_modulate",new Color(instructionsGroup.SelfModulate,1),INSTRUCTION_FADE_SPEED*timeScaleMultiplier);
		instructionsTween.TweenCallback(Callable.From(FadeoutInstructionsText));
	}

	private async void FadeoutModeText(){
		const double MODE_TEXT_DISPLAY_TIME = 1.25;
		const double MODE_TEXT_FADE_OUT_TIME = 0.25;
		await ToSignal(GetTree().CreateTimer(MODE_TEXT_DISPLAY_TIME,true,false,true), "timeout");
		Tween modeTween = CreateTween();
		modeTween.TweenProperty(modeText,"scale",new Vector2(2.25f,2.25f),MODE_TEXT_FADE_OUT_TIME);
		modeTween.TweenProperty(modeGroup,"self_modulate",new Color(modeGroup.SelfModulate,0),MODE_TEXT_FADE_OUT_TIME);
		modeTween.TweenCallback(Callable.From(modeGroup.QueueFree));
	}

	private async void FadeoutInstructionsText(){
		const double INSTRUCTIONS_TEXT_DISPLAY_TIME = 1.25;
		const double INSTRUCTIONS_FADE_OUT_TIME = 1.5;
		await ToSignal(GetTree().CreateTimer(INSTRUCTIONS_TEXT_DISPLAY_TIME,true,false,true), "timeout");
		Tween instructionsTween = CreateTween();
		instructionsTween.TweenProperty(instructionsGroup,"self_modulate",new Color(instructionsGroup.SelfModulate,0),INSTRUCTIONS_FADE_OUT_TIME);
		instructionsTween.TweenCallback(Callable.From(QueueFree));
	}
}