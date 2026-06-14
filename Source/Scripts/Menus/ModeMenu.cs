using Godot;
using System;
using System.Collections.Generic;

public partial class ModeMenu : ScrollableMenu{
    private readonly Color DISABLED_TINT = new Color(0.25f,0.25f,0.25f);
    public static bool ModeToggleMenu = false;
    private int inputId = (int)Game.PlayerDatas[0].InputDevice;
    private readonly Mode.GameMode[] modes = new Mode.GameMode[]{
        Mode.GameMode.Race,
        Mode.GameMode.Deathmatch,
        Mode.GameMode.KingOfTheHill,
        Mode.GameMode.CrownTheKing,
        Mode.GameMode.HotPotato,
        Mode.GameMode.Domination,
        Mode.GameMode.BallinToTheBank,
        Mode.GameMode.TargetTest,
        Mode.GameMode.Survival,
        Mode.GameMode.Golf,
        Mode.GameMode.Soccer,
        Mode.GameMode.Volleyball,
        Mode.GameMode.BombBall,
        Mode.GameMode.Payload,
    };
    
    private Label[] texts;
    private Label descriptionLabel;
    private Sprite2D modeIcon;

    public override void _Ready(){
        descriptionLabel = GetNode<Label>("DescriptionLabel");
        modeIcon = GetNode<Sprite2D>("ModeIcon");
        texts = new Label[modes.Length];
        base._Ready();

        //Locate the current mode's position in the list
        bool foundMode = false;
        for(int i = 0; i < modes.Length; i++){
            if(modes[i] == Game.CurrentMode){
                Selection = i + 1;
                foundMode = true;
                break;
            }
        }
        if(!foundMode) Selection = 1;

        int index = 0;
        if(Selections == null) Selections = GetNode("Selections").GetChildren();
        foreach(Node node in Selections){
            if(node is Label label && index < texts.Length){
                texts[index] = label;
                label.Text = Mode.EnumToString(modes[index]);
                index++;
            }
        }

        UpdateSelectionVisual();
        Tour.ResetPlayerScores();
    }

    public override void _Process(double delta){
        InputChecks(delta, inputId);
        if(inputId < (int)PlayerData.PlayerInputDevice.Mouse && Input.IsActionJustReleased("Y" + inputId)){
            Selection = new Random().Next(1, texts.Length+1);
            UpdateSelectionVisual();
        }
    }

    protected override void MenuChoose(int choice){
        SFX.Play("Confirm");
        int index = Selection - 1; //Adjust for 1-based index
        
        if(ModeToggleMenu){
            Tour.EnabledGameModes[modes[index]] = !Tour.EnabledGameModes[modes[index]];
            UpdateSelectionVisual(); //Refreshes visual states instantly
        }else{
            LevelMenu.FoldersOpened = new List<string> {""};
            Game.CurrentMode = modes[index]; //Set the selected game mode
            MenuScene.LoadMenu("LevelMenu");
        }
    }

    public override void MenuBack(){
        SFX.Play("Back");
        if(IsOnline()){
            OnlineLobby.ShowLobby(true);
            QueueFree();
        }else{
            if(ModeToggleMenu){
                Game.CurrentMode = Mode.GameMode.None;
                MenuScene.LoadMenu("TourMenu");
            }else{
                MenuScene.LoadMenu("VsMenu");
            }
        }
        ModeToggleMenu = false;
    }

    protected override void UpdateSelectionVisual(){
        base.UpdateSelectionVisual(); //CRITICAL: This triggers ScrollableMenu's sliding window movement and scale tweens
        if(texts == null || texts.Length == 0) return;
        
        for(int i = 0; i < texts.Length; i++){
            if(texts[i] != null){
                if(Selection - 1 == i){
                    texts[i].SelfModulate = Tour.EnabledGameModes[modes[i]] ? SELECTED_COLOR : Colors.Red;
                }else{
                    texts[i].SelfModulate = Tour.EnabledGameModes[modes[i]] ? Colors.White : DISABLED_TINT;
                }
            }
        }
        Mode.GameMode mode = modes[Selection-1];
        modeIcon.Texture = GD.Load<Texture2D>("res://Assets/Sprites/Menus/Modes/" + Mode.EnumToString(mode) + " Icon.png");
        descriptionLabel.Text = Mode.GetModeDescription(mode);
    }
    
    private static bool IsOnline(){
        return Game.GameNode.Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer && Game.GameNode.Multiplayer != null;
    }
}