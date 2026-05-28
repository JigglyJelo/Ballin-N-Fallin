using Godot;
using System;

public partial class PlayerInput{
	private Player player;
	private PlayerData.PlayerInputDevice InputDevice; // Player's Controller #0-7, 8 mouse, -1 None (Online player)
	private int InputId;

	public PlayerInput(Player player){
		this.player = player;
		InputDevice = player.PlayerData.InputDevice;
		InputId = (int)InputDevice;
	}

	public void DoPlayerInputs(float delta){
		if(InputDevice > PlayerData.PlayerInputDevice.None && InputDevice < PlayerData.PlayerInputDevice.Mouse){
			ControllerControls(delta);
		}else if(InputDevice == PlayerData.PlayerInputDevice.Mouse){
			MouseControls(delta);
		}
	}
	
	private void ControllerControls(float delta){
		player.RawInputVector = Input.GetVector("Aim Left" + InputId,"Aim Right" + InputId,"Aim Up" + InputId,"Aim Down" + InputId);
		player.InputVector = player.RawInputVector.Normalized();
		
		if(Input.IsActionPressed("Charge N Launch" + InputId) && player.LaunchPower < Player.MAX_LAUNCH_POWER && player.CanLaunch){
			player.LaunchPower += delta * ((Player.MAX_LAUNCH_POWER/Player.MAX_LAUNCH_TIME) * player.GetChargeSpeedMultiplier());
		}
		if(Input.IsActionJustReleased("Charge N Launch" + InputId)) player.Launch();
		if(Input.IsActionJustPressed("Slam" + InputId)) player.Slam();
		if(Input.IsActionJustPressed("Y" + InputId)) player.Visuals.Rpc(nameof(player.Visuals.ShowPlayerText));
		if(Input.IsActionJustPressed("Item" + InputId)) player.ItemButtonPressed();
		if(Input.IsActionJustPressed("Start" + InputId)){
			PauseMenu.Pauser = player.Id;
			Mode.ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Menus/PauseMenu.tscn").Instantiate());
		}
	}
	private void MouseControls(float delta){
		if(Input.GetLastMouseVelocity() != Vector2.Zero || true){
			switch(Game.MouseMode){
				case Game.MouseModeEnum.Cursor:
					player.RawInputVector = player.Rb.GlobalPosition.DirectionTo(player.GetGlobalMousePosition()); //Cursor based aim
					break;
				case Game.MouseModeEnum.Direction:
					MouseAiming(); //Direction based aim
					break;
			}
			player.InputVector = player.RawInputVector;
		}
		
		if(Input.IsActionPressed("Charge N Launch Mouse") && player.LaunchPower < Player.MAX_LAUNCH_POWER && player.CanLaunch){
			player.LaunchPower += delta * ((Player.MAX_LAUNCH_POWER/Player.MAX_LAUNCH_TIME) * player.GetChargeSpeedMultiplier());
		}
		if(Input.IsActionJustReleased("Charge N Launch Mouse")) player.Launch();
		if(Input.IsActionJustPressed("Slam Mouse")) player.Slam();
		if(Input.IsActionPressed("Y Mouse")) player.Visuals.Rpc(nameof(player.Visuals.ShowPlayerText));
		if(player.Item != null && Input.IsActionJustReleased("Item Mouse") && !player.Visuals.ItemRouletteAnimation.Visible) player.ItemButtonPressed();
		if(Input.IsActionPressed("Pause Keyboard")){
			PauseMenu.Pauser = player.Id;
			Mode.ModeNode.AddChild(GD.Load<PackedScene>("res://Source/Scenes/Object Scenes/Menus/PauseMenu.tscn").Instantiate());
		}

		void MouseAiming(){
			// Get the size of the viewport
			Vector2 viewportSize = player.GetViewportRect().Size;
			Vector2 viewportCenter = viewportSize / 2;
			// Get the mouse position relative to the viewport
			Vector2 mousePosition = player.GetViewport().GetMousePosition();
			// Define the radii of the inner and outer circles
			float outerCircleRadius = viewportCenter.Y * 0.3125f; // Adjust as needed
			float innerCircleRadius = outerCircleRadius / 10f;  // Adjust as needed
			// Calculate the vector from the center of the viewport to the mouse position
			Vector2 offset = mousePosition - viewportCenter;
			// Calculate the distance from the center
			float distanceSquared = offset.LengthSquared();//offset.Length();
			// Check if the cursor is outside the outer circle
			if(distanceSquared > outerCircleRadius*outerCircleRadius){
				// Calculate the angle from the center to the mouse position
				float angle = MathF.Atan2(offset.Y, offset.X);
				// Calculate the new position on the outer circle's edge
				float newX = MathF.Cos(angle) * outerCircleRadius;
				float newY = MathF.Sin(angle) * outerCircleRadius;
				// Update the cursor position to the edge of the outer circle
				mousePosition = viewportCenter + new Vector2(newX, newY);
				// Set the cursor position
				player.GetViewport().WarpMouse(mousePosition);
			}else if(distanceSquared < innerCircleRadius*innerCircleRadius){
				// Calculate the angle from the center to the mouse position
				float angle = MathF.Atan2(offset.Y, offset.X);
				// Calculate the new position on the inner circle's edge
				float newX = MathF.Cos(angle) * innerCircleRadius;
				float newY = MathF.Sin(angle) * innerCircleRadius;
				// Update the cursor position to the edge of the inner circle
				mousePosition = viewportCenter - new Vector2(newX, newY);
				// Set the cursor position
				player.GetViewport().WarpMouse(mousePosition);
			}
			// Normalize the raw input vector
			player.RawInputVector = (player.GetGlobalMousePosition() - player.GlobalPosition).Normalized();
		}
	}
}
