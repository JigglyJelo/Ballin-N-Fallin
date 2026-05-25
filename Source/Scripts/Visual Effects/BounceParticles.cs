using Godot;

public partial class BounceParticles : CpuParticles2D{
	public CpuParticles2D DustParticles;
	public override void _Ready(){
		DustParticles = GetNode<CpuParticles2D>("DustParticles");
	}

	public void StartEmitting(Vector2 velocity){
		float velocityAngle = velocity.Angle();
		Rotation = velocityAngle;
		float lerpAmount = (velocity.Length()-1250) / 6000;
		float colorAmount = Mathf.Lerp(-0.15f,0.15f,lerpAmount);
		SelfModulate = colorAmount > 0 ? SelfModulate.Darkened(colorAmount) : SelfModulate.Lightened(-colorAmount); //PlayerColor
		InitialVelocityMin = Mathf.Lerp(225,550,lerpAmount);
		InitialVelocityMax = Mathf.Lerp(265,700,lerpAmount);
		Amount = (int)Mathf.Lerp(3,14,lerpAmount);
		DustParticles.Amount = (int)Mathf.Lerp(4,18,lerpAmount);
		DustParticles.ScaleAmountMin = Mathf.Lerp(0.32f,0.40f,lerpAmount);
		DustParticles.ScaleAmountMax = Mathf.Lerp(0.65f,0.81f,lerpAmount);
		DustParticles.InitialVelocityMin = Mathf.Lerp(80,150,lerpAmount);
		DustParticles.InitialVelocityMax = Mathf.Lerp(140,300,lerpAmount);
		if(Preprocess != 0) DustParticles.Preprocess = Preprocess;
		Emitting = true;
		DustParticles.Emitting = true;
		OneShot = true;
		DustParticles.OneShot = true;
	}
}