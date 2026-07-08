using Godot;
using System;
using System.Collections.Generic;

public partial class MiniControlsMenu : VerticalMenu, ILeftRightSelections{
	public Action OnCanceled;
	public int InputId;
	public string TargetProfile = ControlProfileManager.DEFAULT_PROFILE;

	private Label[] actionLabels;
	private Label profileLabel, vibrationLabel;
	
	private bool isListening = false;
	private float remapCooldown = 0f;

	public override void _Ready(){
		base._Ready();
		
		totalSelections = ControlProfileManager.REMAP_ACTIONS.Length + 1; 
		Selection = 1;
		profileLabel = GetNode<Label>("ProfileText");
		vibrationLabel = GetNode<Label>("Selections/VibrationLabel");
		
		actionLabels = new Label[ControlProfileManager.REMAP_ACTIONS.Length];
		for(int i = 0; i < actionLabels.Length; i++){
			actionLabels[i] = GetNode<Label>($"Selections/ActionLabel_{i}");
		}
	}

	public void Open(int inputId, string profile){
		InputId = inputId;
		TargetProfile = profile;
		Visible = true;
		SetProcess(true);
		isListening = false;
		remapCooldown = 0.2f;
		Selection = 1;
		
		profileLabel.Text = $"Editing {TargetProfile}";
		UpdateSelectionVisual();
		UpdateTexts();
	}

	public override void _Process(double delta){
		if(!Visible) return;

		if(remapCooldown > 0){
			remapCooldown -= (float)delta;
			return;
		}

		if(isListening) return;
		InputChecks(delta, InputId);
	}

	protected override void MenuChoose(int choice){
		if(TargetProfile == ControlProfileManager.DEFAULT_PROFILE){
			SFX.Play("Error");
			return; 
		}

		if(choice == totalSelections){ 
			bool currentVibration = ControlProfileManager.GetVibration(TargetProfile);
			ControlProfileManager.SetVibration(TargetProfile, !currentVibration);
			SFX.Play("Confirm");
			UpdateTexts();
			return;
		}

		SFX.Play("Confirm");
		isListening = true;
		
		int actionIndex = choice - 1;
		actionLabels[actionIndex].Text = ControlProfileManager.REMAP_ACTIONS[actionIndex] + ": [Press Button to Add]";
	}

	public override void MenuBack(){
		SFX.Play("Back");
		Visible = false;
		SetProcess(false);
		OnCanceled?.Invoke();
	}

	public void MenuRight(){ ToggleVibration(); }
	public void MenuLeft(){ ToggleVibration(); }

	private void ToggleVibration(){
		if(Selection == totalSelections){ 
			if(TargetProfile == ControlProfileManager.DEFAULT_PROFILE){
				SFX.Play("Error");
				return;
			}
			bool currentVibration = ControlProfileManager.GetVibration(TargetProfile);
			ControlProfileManager.SetVibration(TargetProfile, !currentVibration);
			SFX.Play("Move", Game.Random.Next(80, 110) / 100f);
			joystickTimer = 0;
			UpdateTexts();
		}
	}

	public override void _Input(InputEvent @event){
		if(!Visible) return;

		if(@event is InputEventJoypadButton joyBtn && joyBtn.Device != InputId) return;
		if(@event is InputEventJoypadMotion joyMot && joyMot.Device != InputId) return;

		if(!isListening){
			if(Selection < totalSelections && @event is InputEventJoypadButton btnEvent && btnEvent.IsPressed()){
				if(TargetProfile == ControlProfileManager.DEFAULT_PROFILE && (btnEvent.ButtonIndex == JoyButton.X || btnEvent.ButtonIndex == JoyButton.Y)){
					SFX.Play("Error");
					GetViewport().SetInputAsHandled();
					return;
				}

				int actionIndex = Selection - 1;
				string targetAction = ControlProfileManager.REMAP_ACTIONS[actionIndex];

				if(btnEvent.ButtonIndex == JoyButton.X){ 
					ControlProfileManager.ClearEventsFromProfile(TargetProfile, targetAction);
					SFX.Play("Move"); 
					UpdateTexts();
					GetViewport().SetInputAsHandled();
				}else if(btnEvent.ButtonIndex == JoyButton.Y){ 
					ControlProfileManager.RestoreFactoryDefault(TargetProfile, targetAction);
					SFX.Play("Confirm");
					UpdateTexts();
					GetViewport().SetInputAsHandled();
				}
			}
			return;
		}

		if(@event is InputEventJoypadButton joyEvent && joyEvent.IsPressed()){
			if(joyEvent.ButtonIndex == JoyButton.Start || joyEvent.ButtonIndex == JoyButton.Back){
				SFX.Play("Error");
				return;
			}
			AssignNewBind(joyEvent);
		}else if(@event is InputEventJoypadMotion joyMotion){
			if((joyMotion.Axis == JoyAxis.TriggerLeft || joyMotion.Axis == JoyAxis.TriggerRight) && joyMotion.AxisValue > 0.5f){
				AssignNewBind(joyMotion);
			}
		}
	}

	private void AssignNewBind(InputEvent newEvent){
		int actionIndex = Selection - 1;
		string targetAction = ControlProfileManager.REMAP_ACTIONS[actionIndex];

		ControlProfileManager.AddEventToProfile(TargetProfile, targetAction, newEvent);
		
		isListening = false;
		remapCooldown = 0.2f;
		GetViewport().SetInputAsHandled();
		UpdateTexts();
		SFX.Play("Confirm");
	}

	private void UpdateTexts(){
		Godot.Collections.Dictionary profileData = ControlProfileManager.GetProfileData(TargetProfile);

		for(int i = 0; i < ControlProfileManager.REMAP_ACTIONS.Length; i++){
			string action = ControlProfileManager.REMAP_ACTIONS[i];
			string keyName = "Unassigned";
			List<string> bindNames = new List<string>();

			bool useDefault = TargetProfile == ControlProfileManager.DEFAULT_PROFILE || !profileData.ContainsKey(action);

			if(!useDefault){
				Variant savedData = profileData[action];
				if(savedData.Obj is Godot.Collections.Array godotArray && godotArray.Count > 0){
					foreach(Variant v in godotArray){
						if(v.Obj is string savedString){
							InputEvent reconstructedEvent = ControlProfileManager.DeserializeEvent(savedString);
							if(reconstructedEvent != null) bindNames.Add(FormatEventName(reconstructedEvent));
						}
					}
					keyName = string.Join(", ", bindNames);
				}
			}
			
			if(useDefault){
				Godot.Collections.Array<InputEvent> defaultEvents = ControlProfileManager.GetHardcodedDefaults(action);
				if(defaultEvents.Count > 0){
					foreach(InputEvent defEvent in defaultEvents) bindNames.Add(FormatEventName(defEvent));
					keyName = string.Join(", ", bindNames);
				}
			}

			actionLabels[i].Text = $"{action}: {keyName}";
		}

		bool vibrationEnabled = ControlProfileManager.GetVibration(TargetProfile);
		vibrationLabel.Text = $"Vibration: {(vibrationEnabled ? "On" : "Off")}";
	}

	private string FormatEventName(InputEvent @event){
		string rawName = @event.AsText();
		int bracketIndex = rawName.IndexOf(" (");
		if(bracketIndex > 0) rawName = rawName.Substring(0, bracketIndex);
		return rawName;
	}
}