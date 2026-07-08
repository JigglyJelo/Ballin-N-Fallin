using Godot;
using System;
using System.Collections.Generic;

public partial class MiniProfileMenu : ScrollableMenu{
	public Action<string> OnProfileSelected;
	public Action OnCanceled;

	private const float LABEL_X_POS = -1920f;
	private const float LABEL_Y_POS = -400;
	private const float LABEL_SPACING = 200;
	private PackedScene profileLabelScene = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn");

	private Keypad keypadPopup;
	public bool IsKeypadOpen = false; 
	public int InputId = 0;
	private MiniControlsMenu controlsMenu;
	private InputPrompts inputPrompts;
	
	private Godot.Collections.Dictionary<InputPrompts.InputPrompt,string> profilePrompts = new(){
		{ InputPrompts.InputPrompt.North, "Edit" },
		{ InputPrompts.InputPrompt.West, "Delete" },
		{ InputPrompts.InputPrompt.South, "Confirm" },
		{ InputPrompts.InputPrompt.East, "Back" },
	};
	private Godot.Collections.Dictionary<InputPrompts.InputPrompt,string> keypadPrompts = new(){
		{ InputPrompts.InputPrompt.South, "Confirm" },
		{ InputPrompts.InputPrompt.East, "Back" },
	};
	private Godot.Collections.Dictionary<InputPrompts.InputPrompt,string> controlsPrompts = new(){
		{ InputPrompts.InputPrompt.North, "Reset" },
		{ InputPrompts.InputPrompt.West, "Clear" },
		{ InputPrompts.InputPrompt.South, "Confirm" },
		{ InputPrompts.InputPrompt.East, "Back" },
	};
	
	public bool InControlsMenu = false;

	public override void _Ready(){
		base._Ready();
		
		inputPrompts = GetNode<InputPrompts>("InputPrompts");
		inputPrompts.InputMessages = profilePrompts;
		
		keypadPopup = GetNode<Keypad>("Keypad");
		keypadPopup.Visible = false;
		keypadPopup.OnTagConfirmed += HandleTagConfirmed;
		keypadPopup.OnCanceled += HandleKeypadCanceled;

		controlsMenu = GetNode<MiniControlsMenu>("MiniControlsMenu");
		controlsMenu.Visible = false;
		controlsMenu.OnCanceled += HandleControlsCanceled;
		defaultFontSize = 0.4f;
	}

	public void Open(int inputId, string currentProfile){
		InputId = inputId;
		IsKeypadOpen = false;
		InControlsMenu = false; 
		inputPrompts.InputMessages = profilePrompts;
		RefreshProfileList();

		int index = ControlProfileManager.Profiles.IndexOf(currentProfile);
		if(index >= 0){
			Selection = index + 2; 
			UpdateSelectionVisual();
		}
	}

	public void OpenControlsMenu() {
		if(Selection > 1) { 
			string selectedProfile = ControlProfileManager.Profiles[Selection - 2];
			InControlsMenu = true;
			if(selectionsContainer != null) selectionsContainer.Visible = false;
			controlsMenu.Open(InputId, selectedProfile);
			inputPrompts.InputMessages = controlsPrompts;
			SFX.Play("Confirm");
		}
	}

	public override void _Process(double delta){
		if(!Visible || IsKeypadOpen || InControlsMenu) return;
		
		InputChecks(delta, InputId); 

		if(Input.IsActionJustReleased("Slam" + InputId)){
			if(Selection > 1){ 
				string selectedProfile = ControlProfileManager.Profiles[Selection - 2];
				if(selectedProfile != ControlProfileManager.DEFAULT_PROFILE){
					ControlProfileManager.DeleteProfile(selectedProfile); 
					SFX.Play("Move"); 
					RefreshProfileList();
				}else{
					SFX.Play("Error"); 
				}
			}
		}
	}

	private void HandleControlsCanceled(){
		InControlsMenu = false;
		if(selectionsContainer != null) selectionsContainer.Visible = true;
		inputPrompts.InputMessages = profilePrompts;
	}

	private void RefreshProfileList(){
		if(selectionsContainer == null) return;
		foreach(Node child in selectionsContainer.GetChildren()){
			selectionsContainer.RemoveChild(child);
			child.QueueFree();
		}

		Selections = new Godot.Collections.Array<Node>();
		float currentY = LABEL_Y_POS;

		Label createLabel = profileLabelScene.Instantiate<Label>();
		createLabel.Text = "+ Create New Profile";
		createLabel.Position = new Vector2(LABEL_X_POS, currentY);
		selectionsContainer.AddChild(createLabel);
		Selections.Add(createLabel);
		currentY += LABEL_SPACING * defaultFontSize;

		foreach(string profile in ControlProfileManager.Profiles){
			Label profileLabel = profileLabelScene.Instantiate<Label>();
			profileLabel.Text = profile;
			profileLabel.Position = new Vector2(LABEL_X_POS, currentY);
			selectionsContainer.AddChild(profileLabel);
			Selections.Add(profileLabel);
			currentY += LABEL_SPACING * defaultFontSize;
		}

		totalSelections = Selections.Count;
		if(Selection > totalSelections) Selection = totalSelections; 
		UpdateSelectionVisual();
	}

	protected override void MenuChoose(int choice){
		if(choice == 1){ 
			IsKeypadOpen = true;
			if(selectionsContainer != null) selectionsContainer.Visible = false;
			keypadPopup.Open(InputId); 
			inputPrompts.InputMessages = keypadPrompts;
			SFX.Play("Confirm");
		}else{ 
			string selectedProfile = ControlProfileManager.Profiles[choice - 2];
			SFX.Play("Confirm");
			OnProfileSelected?.Invoke(selectedProfile);
		}
	}

	public override void MenuBack(){
		SFX.Play("Back");
		OnCanceled?.Invoke();
	}

	private void HandleTagConfirmed(string newTag){
		IsKeypadOpen = false;
		keypadPopup.Close();
		if(selectionsContainer != null) selectionsContainer.Visible = true;
		
		ControlProfileManager.CreateProfile(newTag); 
		SFX.Play("Confirm", 1.125f);
		inputPrompts.InputMessages = profilePrompts;
		RefreshProfileList();

		int newIndex = ControlProfileManager.Profiles.IndexOf(newTag);
		if(newIndex >= 0){
			Selection = newIndex + 2;
			UpdateSelectionVisual();
		}
	}

	private void HandleKeypadCanceled(){
		IsKeypadOpen = false;
		keypadPopup.Close();
		if(selectionsContainer != null) selectionsContainer.Visible = true;
		inputPrompts.InputMessages = profilePrompts;
		SFX.Play("Back", 1.125f);
	}
}