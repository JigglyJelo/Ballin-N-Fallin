using Godot;

public partial class CreditsPanel : Panel{
	private const float SCROLL_SPEED = 2000;
	private ScrollContainer scrollContainer;
	private VScrollBar vScroll;
	private Label licenseLabel;
	public string LicenseText{
		get{return licenseLabel.Text;}
		set{
			licenseLabel.Text = value;
			vScroll.Value = 0;
		}
	}

	public override void _Ready(){
		scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
		vScroll = scrollContainer.GetVScrollBar();
		licenseLabel = scrollContainer.GetNode<Label>("CreditsLabel");
	}

	public override void _PhysicsProcess(double delta){
		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			float y = Input.GetVector("Aim Left" + i, "Aim Right" + i, "Aim Up" + i, "Aim Down" + i).Y;
			if(y > 0.5f){
				vScroll.Value += SCROLL_SPEED * delta;
				return;
			}else if(y < -0.5f){
				vScroll.Value -= SCROLL_SPEED * delta;
				return;
			}
		}
	}
}