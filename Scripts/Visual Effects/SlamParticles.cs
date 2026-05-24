using Godot;

public partial class SlamParticles : Sprite2D {
	private Sprite2D big, medium;
	public float Preprocess = 0;
	private float startTime = 0.1f;
	private float middleTweenTime = 0.02f;
	private float remainingStartTime;
	private float remainingMiddleTweenTime;
	private float remainingEndTweenTime;
	private float remainingBigTweenTime = 0.02f;  // Add this for big phase

	public override void _Ready() {
		medium = GetNode<Sprite2D>("Star");
		big = GetNode<Sprite2D>("Diamond");

		// Initial settings for big and medium
		big.Scale = Vector2.Zero;
		medium.Scale = Vector2.Zero;
		medium.SelfModulate = new Color(200/255f, 200/255f, 200/255f, 0);
		big.SelfModulate = new Color(128 / 255f, 128 / 255f, 128 / 255f, 0);

		// Set initial state based on Preprocess value
		SetState();
	}

	private async void ShowMedium() {
		await ToSignal(GetTree().CreateTimer(remainingStartTime > 0 ? remainingStartTime : startTime, false), "timeout");
		Tween middleTween = CreateTween();
		middleTween.TweenProperty(medium, "self_modulate", new Color(200/255f, 200/255f, 200/255f, 1), remainingMiddleTweenTime > 0 ? remainingMiddleTweenTime : middleTweenTime);
		middleTween.TweenProperty(medium, "scale", new Vector2(1, 1), remainingMiddleTweenTime > 0 ? remainingMiddleTweenTime : middleTweenTime);
		middleTween.TweenCallback(new Callable(this, nameof(ShowBig)));
	}

	private void ShowBig() {
		Tween bigTween = CreateTween();
		bigTween.TweenProperty(big, "self_modulate", new Color(128 / 255f, 128 / 255f, 128 / 255f, 1), remainingBigTweenTime > 0 ? remainingBigTweenTime : 0.02f);
		bigTween.TweenProperty(big, "scale", new Vector2(1, 1), remainingBigTweenTime > 0 ? remainingBigTweenTime : 0.02f);
		bigTween.TweenCallback(new Callable(this, nameof(FadeOut)));
	}

	private void FadeOut() {
		Tween endTween = CreateTween();
		endTween.TweenProperty(this, "scale", Vector2.Zero, remainingEndTweenTime > 0 ? remainingEndTweenTime : 0.125f);
		endTween.TweenCallback(new Callable(this, nameof(PuttingQueueFreeInCallableDoesntWork)));
	}
	private void PuttingQueueFreeInCallableDoesntWork(){
		QueueFree();
	}

	private void SetState() {
		// Reset remaining times
		remainingStartTime = startTime;
		remainingMiddleTweenTime = middleTweenTime;
		remainingEndTweenTime = 0.125f;
		remainingBigTweenTime = 0.02f;

		if (Preprocess < startTime) {
			// Fast-forward the time within the medium part's animation
			remainingStartTime = startTime - Preprocess;
			ShowMedium();  // Fast-forward medium part
		} else if (Preprocess < startTime + middleTweenTime) {
			// Fast-forward through medium, start animating the big part with reduced time
			float remainingTimeForBig = Preprocess - startTime;
			remainingMiddleTweenTime = middleTweenTime - remainingTimeForBig;
			ShowMedium();  // Still show medium, but it'll be faster
		} else {
			// Fast-forward to the fade-out phase
			float remainingTimeForFadeOut = Preprocess - (startTime + middleTweenTime);
			remainingEndTweenTime = 0.125f - remainingTimeForFadeOut;
			remainingBigTweenTime = 0.02f; // Already past medium, so default big tween time is used
			ShowMedium();  // Start the sequence but fast-forward through
		}
	}
}