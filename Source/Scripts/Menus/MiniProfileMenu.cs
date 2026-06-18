using Godot;
using System;

public partial class MiniProfileMenu : ScrollableMenu{
	public Action<string> OnProfileSelected;
	public Action OnCanceled;

	private const float LABEL_X_POS = -1920f;
    private const float LABEL_Y_POS = -800;
	private const float LABEL_SPACING = 200;
    private readonly Vector2 LABEL_SCALE = new Vector2(0.4f,0.4f);
	private PackedScene profileLabelScene = GD.Load<PackedScene>(MenuScene.MENU_PATH + "LevelLabel.tscn");

	private Keypad keypadPopup;
	public bool IsKeypadOpen = false; 
	public int InputId = 0;

	private MiniControlsMenu controlsMenu;
	private bool inControlsMenu = false;

	public override void _Ready(){
		base._Ready();
		
		keypadPopup = GetNode<Keypad>("Keypad");
		keypadPopup.Visible = false;
		keypadPopup.OnTagConfirmed += HandleTagConfirmed;
		keypadPopup.OnCanceled += HandleKeypadCanceled;

		controlsMenu = GetNode<MiniControlsMenu>("MiniControlsMenu");
		controlsMenu.Visible = false;
		controlsMenu.OnCanceled += HandleControlsCanceled;
	}

	public void Open(int inputId, string currentProfile){
		InputId = inputId;
		IsKeypadOpen = false;
		inControlsMenu = false; 
		RefreshProfileList();

		int index = ControlProfileManager.Profiles.IndexOf(currentProfile);
		if(index >= 0){
			Selection = index + 2; 
			UpdateSelectionVisual();
		}
	}

	public override void _Process(double delta){
		if(!Visible || IsKeypadOpen || inControlsMenu) return;
		
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

		if(Input.IsActionJustReleased("Start" + InputId)){
			if(Selection > 1){ 
				string selectedProfile = ControlProfileManager.Profiles[Selection - 2];
				inControlsMenu = true;
				if(selectionsContainer != null) selectionsContainer.Visible = false;
				controlsMenu.Open(InputId, selectedProfile);
				SFX.Play("Confirm");
			}
		}
	}

	private void HandleControlsCanceled(){
		inControlsMenu = false;
		if(selectionsContainer != null) selectionsContainer.Visible = true;
	}

	private void RefreshProfileList(){
		if(selectionsContainer == null) return;
        selectionsContainer.Scale = LABEL_SCALE;
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
		currentY += LABEL_SPACING;

		foreach(string profile in ControlProfileManager.Profiles){
			Label profileLabel = profileLabelScene.Instantiate<Label>();
			profileLabel.Text = profile;
			profileLabel.Position = new Vector2(LABEL_X_POS, currentY);
			selectionsContainer.AddChild(profileLabel);
			Selections.Add(profileLabel);
			currentY += LABEL_SPACING;
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
		SFX.Play("Back", 1.125f);
	}
}