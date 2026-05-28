using Godot;
using System;

public class PlayerPhysics{
    public const float MAX_LAUNCH_TIME = 1.25f; // Amount of time it takes to fully charge
	public const float MAX_LAUNCH_POWER = 4000; // The maximum amount of power added onto a launch based on charge time (Plus the MIN_LAUNCH_POWER)
	public const float MIN_LAUNCH_POWER = 500; // Amount of power always added onto a launch no matter time held
	public const float SLAM_POWER = 2000;
	public const float GRAVITY = 2;
	public const float MASS = 1;
	public const float LINEAR_DAMP = 0.2f;
	public const float ANGULAR_DAMP = 0.125f;
	public const float FRICTION = 1;
	public const float BOUNCE = 0.65f; // The bounciness of the player
	public const float RADIUS = 91; // The radius of the player
	public const float SPEED_CAP = 10000; //The maximum velocity in any direction
	public const float BOUNCE_SFX_TIMEOUT = 0.15f;
	public const float MIN_STOMP_SPEED = (float)(SLAM_POWER*0.5); //Minimum velocity downwards you must be going to stay in stomping state
	public const float MIN_VEL_FOR_LAUNCH_PARTICLE = (float)(MAX_LAUNCH_POWER * 0.15); //The amount your launch needed to be charged for particle to spawn
	private const float MIN_STRETCH_SPEED = 2300; //The minimum velocity before the squash n stretch effect starts
    public float StillTimer, AirTimer;
	private Player player;
    public PlayerPhysics(Player player){
        this.player = player;
    }
    public void DoPlayerPhysics(float delta){
        float velocityMagnitudeSquared = player.Rb.LinearVelocity.LengthSquared();
		if(Online.IsHost()){
			if((player.Rb.GlobalPosition.Y>2500/Level.LevelNode.CameraZoom || (player.Rb.GlobalPosition.Y<-2500/Level.LevelNode.CameraZoom && player.Rb.GlobalPosition.Y>-10000/Level.LevelNode.CameraZoom) || (player.Rb.GlobalPosition.X<-4444/Level.LevelNode.CameraZoom && player.Rb.GlobalPosition.X>-17777/Level.LevelNode.CameraZoom) || (player.Rb.GlobalPosition.X>4444/Level.LevelNode.CameraZoom && player.Rb.GlobalPosition.X<17777/Level.LevelNode.CameraZoom)) && !player.Finished) 
				Death.KillPlayer(player,Death.DeathCause.Pop);//RespawnPlayer(); //Incase you clip oob
			if(velocityMagnitudeSquared > SPEED_CAP*SPEED_CAP){ //Speed cap
				float angle = player.Rb.LinearVelocity.Angle();
				player.Rb.LinearVelocity = Vector2.FromAngle(angle) * SPEED_CAP;
			}
		}
		
		if(velocityMagnitudeSquared >= (MIN_STRETCH_SPEED*MIN_STRETCH_SPEED)){//&& !isRegaining
			player.Visuals.SquashNStretch(MathF.Sqrt(velocityMagnitudeSquared));
			if(player.Rb.AngularDamp != 25 && (velocityMagnitudeSquared >= (MIN_STRETCH_SPEED*2 * MIN_STRETCH_SPEED*2) || player.Rb.AngularVelocity > 5 || player.Rb.AngularVelocity < -5)){
				player.Rb.AngularDamp = 25;
			}else{
				player.Rb.AngularDamp = Mathf.Lerp(player.Rb.AngularDamp,ANGULAR_DAMP,0.5f);
			}
		}else{
			//float lerpScale = 1-(velocityMagnitudeSquared / ((MIN_STRETCH_SPEED*MIN_STRETCH_SPEED)));
			player.Rb.AngularDamp = Mathf.Lerp(player.Rb.AngularDamp,ANGULAR_DAMP,0.125f);
			player.Visuals.ResetSquashNStretch();
		}

		if(player.IsStomping){
			player.StompTimer += delta;
			const float MIN_STOMP_TIME = 0.25f;
			if(player.Rb.LinearVelocity.Y <= MIN_STOMP_SPEED && player.StompTimer >= MIN_STOMP_TIME){
				player.IsStomping = false;
			}
		}

		//Gives player launch and slam back if stuck in air
		if(!player.CanLaunch || !player.CanSlam){
			if(AirTimer <= 10) AirTimer += delta;
			else{
				player.CanSlam = true;
				player.CanLaunch = true;
				AirTimer = 0;
			}
		}
    }

    public void OnRigidBodyEntered(PhysicsBody2D body){
		if(body.IsInGroup("NoRegain")){
			player.BounceEffects();
			if(body.IsInGroup("Bump")) player.PlayerEmotion = Player.Emotion.Bumped;
			if(player.IsStomping && player.Rb.LinearVelocity.Y <= MIN_STOMP_SPEED){
				//IsStomping = false;
			}
		}else if(body.IsInGroup("Regain") || body.GetParent().IsInGroup("Regain")){
			if(!player.IsRegaining) player.BounceEffects();
			player.IsRegaining = true;
			Mode.ModeNode.OnPlayerEnterRegain(player);
			StillTimer = 0;
			AirTimer = 0;
			if(player.IsStomping && player.Rb.LinearVelocity.Y <= MIN_STOMP_SPEED){
				//IsStomping = false;
			}
		}else if(body.IsInGroup("Bump")){
			player.PlayerEmotion = Player.Emotion.Bumped;
			if(player.IsStomping && player.Rb.LinearVelocity.Y <= MIN_STOMP_SPEED){
				//IsStomping = false;
			}
		}
		if(body.GetParent().IsInGroup("Player")){
			Player otherPlayer = body.GetParent() as Player;
			if(isSuccessfulStomp()){ //Checks whether the player is being stomped on
				if(Online.IsHost()){
					//Stomp is successful
					Mode.ModeNode.PlayerKilledPlayer(player, otherPlayer, Death.DeathCause.Stomp);
					Death.KillPlayer(player,Death.DeathCause.Stomp);
					otherPlayer.Rpc(nameof(otherPlayer.PlayerStomped));
					otherPlayer.Rb.LinearVelocity = new Vector2(otherPlayer.Rb.LinearVelocity.X,-2000f);
				}
			}else{
				if(Online.IsHost()) Mode.ModeNode.PlayerBumpedPlayer(otherPlayer,player);
				player.PlayerEmotion = Player.Emotion.Bumped;
        		otherPlayer.PlayerEmotion = Player.Emotion.Bumped;
			}

			bool isSuccessfulStomp(){
				return Game.StompSetting != Game.StompSettingEnum.Off && otherPlayer.IsStomping && //Make sure player is slaming
				player.Rb.GlobalPosition.Y >= otherPlayer.Rb.GlobalPosition.Y && //Stomping player above stomped player
				MathF.Abs(player.Rb.GlobalPosition.X-otherPlayer.Rb.GlobalPosition.X) <= 150f && //Stomping player is horizontally aligned with stomper
				player.PlayerScale <= otherPlayer.PlayerScale && //Stomping player is bigger than or equal to stomped scale
				!Level.IsPositionOffscreenOrDead(player.Rb.GlobalPosition) && //Can't stomp dead (or i guess offscreen) players
				(player.Team.Equals("") || ((!player.Team.Equals("") && !player.Team.Equals(otherPlayer.Team)) || Game.StompSetting == Game.StompSettingEnum.TeamAttack)); //Stomping player is on another team or there are no teams
			}

			//Unfreeze frozen players on collision
			if(otherPlayer.Rb.Freeze && !otherPlayer.Finished){
				otherPlayer.Rb.SetDeferred("freeze",false);
				GD.Print("Unfrozen");
			}
			player.PlayerInput.ApplyVibration();
		}
	}

	public void OnRigidBodyExited(PhysicsBody2D body){
		if(body.IsInGroup("Regain") || body.GetParent().IsInGroup("Regain")){
			player.IsRegaining = false;
		}
	}

	public void ApplyLaunch(){
	    // If current velocity is opposite to launch direction, reduce velocity
	    float diff = player.Rb.LinearVelocity.AngleTo(player.InputVector);
	    //Normalize the angle difference to range -π to π
	    diff = MathF.Atan2(MathF.Sin(diff), MathF.Cos(diff));
	    if(MathF.Abs(diff) >= (float)(Math.PI/4)){
	        player.Rb.LinearVelocity *= 0.5f; // Less aggressive reduction
	    }
	    //Apply launch force
		//Rb.ApplyImpulse(InputVector * (LaunchPower + MIN_LAUNCH_POWER));
	    player.Rb.LinearVelocity += player.InputVector * (player.LaunchPower + MIN_LAUNCH_POWER);
	}

	public void ApplySlam(){
		if(player.Rb.LinearVelocity.Y < 0) player.Rb.LinearVelocity = new Vector2(player.Rb.LinearVelocity.X, player.Rb.LinearVelocity.Y * 0.5f);
		player.Rb.LinearVelocity += Vector2.Down * SLAM_POWER * (player.Rb.GravityScale == 0 ? 1 : player.Rb.GravityScale/GRAVITY);
	}

	public void UpdateRadius(){
		CircleShape2D defaultCircle = new CircleShape2D();
		defaultCircle.Radius = RADIUS * player.PlayerScale;
		player.RbShape.Shape = defaultCircle;
	}

	public void ResetPhysicsTransformations(){
		player.Rb.GravityScale = GRAVITY;
		player.Rb.Mass = MASS;
		player.Rb.LinearDamp = LINEAR_DAMP;
		player.Rb.AngularDamp = ANGULAR_DAMP;
		PhysicsMaterial physicsMaterial = new PhysicsMaterial();
		physicsMaterial.Friction = FRICTION;
		physicsMaterial.Bounce = BOUNCE;
		player.Rb.PhysicsMaterialOverride = physicsMaterial;
	}

	public void SetPlayerCollisionExceptions(bool ignore){
    	foreach(Player otherPlayer in Game.Players){
    	    if(otherPlayer != null && otherPlayer != player){
    	        if(ignore) player.Rb.AddCollisionExceptionWith(otherPlayer.Rb);
    	        else player.Rb.RemoveCollisionExceptionWith(otherPlayer.Rb);
    	    }
    	}
	}
}