using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ColorMenu : Menu2D{
    private int selectionX = 0;
    private int selectionY = 0;
	public int Id;
    public int InputId;
	public static int JoinedPlayers = 0;
	public static int ReadyPlayers = 0;
    private bool isReady = false;
	private Sprite2D playerBallSprite, playerShadingSprite, colorCursor;
    private Label profileLabel;
    private Polygon2D colorBG;
    private Sprite2D[,] colorButtons = new Sprite2D[3,4];
	private Label colorText;
    private float firstClickTimer = 0;
    private const float FIRST_CLICK_TIMEOUT = 0.25f;
    public static List<Color> DefaultColorOrder = new List<Color>{
        Game.Colors["Orange"],
        Game.Colors["Lime"],
        Game.Colors["Cyan"],
        Game.Colors["Pink"],
        Game.Colors["Yellow"],
        Game.Colors["White"],
        Game.Colors["Gray"],
        Game.Colors["Brown"],
        Game.Colors["Red"]
    };
    
    private readonly Color[,] COLOR_ARRAY = new Color[3, 4]{
        {Game.Colors["Orange"], Game.Colors["Lime"], Game.Colors["Cyan"], Game.Colors["Pink"]},
        {Game.Colors["Red"], Game.Colors["Green"], Game.Colors["Blue"], Game.Colors["Purple"]},
        {Game.Colors["Yellow"], Game.Colors["White"], Game.Colors["Gray"], Game.Colors["Brown"]}
    };
    
	public override void _Ready(){
        //Set input id
        if(IsOnline()){
            InputId = (int)Online.InputId;
            if(!Game.UsingMouse() && Online.InputId == PlayerData.PlayerInputDevice.Mouse){
                Game.MouseMode = Game.MouseModeEnum.Cursor;
            }
        }else{
            if(!Game.UsingMouse()) InputId = (int)Game.PlayerDatas[Id-1].InputDevice;
            else InputId = (int)PlayerData.PlayerInputDevice.Mouse;
        }
        
		playerBallSprite = GetNode<Sprite2D>("Player/BallSprite");
        playerShadingSprite = GetNode<Sprite2D>("Player/ShadingSprite");
		colorCursor = GetNode<Sprite2D>("ColorBackground/ColorSelector");
		colorBG = GetNode<Polygon2D>("ColorBackground");
        colorText = GetNode<Label>("Color Text");
        profileLabel = GetNode<Label>("ColorBackground/ProfileLabel");
        JoinedPlayers++;
        
        //Sets default color if Controller's first time joining
        if(!Online.IsOnline) Game.PlayerDatas[Id-1].PlayerColor = DefaultColorOrder.First(color => !Game.PlayerDatas.Any(player => player.PlayerColor == color));
        else{
            Id = Game.PlayerDatas.FindIndex(player => player.UUID == Game.GameNode.Multiplayer.GetUniqueId())+1;
        }
        //Controller
        if(!Game.UsingMouse()){
            profileLabel.Text = "Profile: " + Game.PlayerDatas[Id-1].ControlProfileName;
            //Sets player's initial color cursor position
		    for(int i = 0; i < COLOR_ARRAY.GetLength(0); i++){
                for(int j = 0; j < COLOR_ARRAY.GetLength(1);j++){
                    if(COLOR_ARRAY[i,j].Equals(Game.GetPlayerColor(Id))){
                        selectionX = j;
                        selectionY = i;
                        UpdateCursorPosition();
                        break;
                    }
                }
            }
        }else if(Game.UsingMouse()){ //Mouse
            colorCursor.Visible = false;
            profileLabel.Visible = false;
            GetNode<Sprite2D>("ColorBackground/Y").Visible = false;
            //Instantiate buttons
            int index = 0;
            string[] keys = new string[Game.Colors.Count];
            for(int i = 0; i < keys.Length; i++) keys[i] = Game.Colors.Keys.ToArray()[i];
            for (int i = 0; i < colorButtons.GetLength(0); i++){
                for (int j = 0; j < colorButtons.GetLength(1); j++){
                    colorButtons[i, j] = GetNode<Sprite2D>("ColorBackground/" + keys[index++]);
                }
            }
        }
        SetPlayerColor(Game.PlayerDatas[Id-1].PlayerColor);
        
        if(Game.TotalPlayers > 4) Scale = new Vector2(0.7f,0.7f);

        if(IsOnline()) Position = new Vector2(0,300);
        else SetPosition();
	}

	public override void _Process(double delta){
        if(!Game.UsingMouse()){
            //Controller controls
            InputChecks(delta,InputId);
            //Vibration Toggle
            //Moment where order of &'s matter (Game.InputIds.Count >= Id) Needed to prevent oob error
            //This still gets called once when being freed which would cause an error trying to access no longer existant Id in InputIds
            if((Game.PlayerDatas.Count >= Id || IsOnline()) && Input.IsActionJustReleased("Y" + InputId)){  //Game.InputIds[Id-1]
		    	SwitchProfile();
            }
        }else{
            //Mouse Controls
            if(!isReady){
                firstClickTimer += (float)delta;
                foreach(Sprite2D colorButton in colorButtons){
                    Vector2 buttonPosition = colorButton.GlobalPosition;
                    if(Mathf.Abs(buttonPosition.DistanceTo(GetGlobalMousePosition())) < 90){
                        Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
                        if(playerBallSprite.SelfModulate != Game.Colors[colorButton.Name]){
                            SFX.Play("Move");
                            SetPlayerColor(Game.Colors[colorButton.Name]);
                        }
                        
                        if(Input.IsActionJustReleased("Charge N Launch Mouse") && firstClickTimer > FIRST_CLICK_TIMEOUT){
                            MenuChoose(0); //free parameter
                        }
                    }
                }
            }
            
            if(Input.IsActionJustReleased("Slam Mouse")) MenuBack();
        }
	}

	protected override void MenuLeft(){
        if(!isReady){
            if(selectionX > 0) selectionX--;
            else selectionX = 3;
            UpdateCursorPosition();
            UpdateSelectionVisual();
        }
    }

    protected override void MenuRight(){
        if(!isReady){
            if(selectionX < 3) selectionX++;
            else selectionX = 0;
            UpdateCursorPosition();
            UpdateSelectionVisual();
        }
    }

    protected override void MenuDown(){
        if(!isReady){
            if(selectionY < 2) selectionY++;
            else selectionY = 0;
            UpdateCursorPosition();
            UpdateSelectionVisual();
        }
    }

    protected override void MenuUp(){
        if(!isReady){
            if(selectionY > 0) selectionY--;
            else selectionY = 2;
            UpdateCursorPosition();
            UpdateSelectionVisual();
        }
    }

    protected override void MenuChoose(int selection){
        //Can't choose color already selected
        if(!IsOnline()){
            if(!isReady && !PlayerMenu.selectedColors.Contains(playerBallSprite.SelfModulate)){
                Game.PlayerDatas[Id-1].PlayerColor = playerBallSprite.SelfModulate;
                PlayerMenu.selectedColors.Add(playerBallSprite.SelfModulate);
                colorText.Text = "Ready";
			    colorBG.Visible = false;
                colorCursor.Visible = false;
                
                ReadyPlayers++;
                isReady = true;
                SFX.Play("Confirm",1.125f);
            }
        }else{
            if(Game.UsingMouse()){
                OnlineLobby.Lobby.RpcId(1,nameof(OnlineLobby.Lobby.SwitchColor),playerBallSprite.SelfModulate);
            }else{
                OnlineLobby.Lobby.RpcId(1,nameof(OnlineLobby.Lobby.SwitchColor),COLOR_ARRAY[selectionY, selectionX]);
            }
            SFX.Play("Confirm",1.125f);
            MenuBack();
        }
    }

    public override void MenuBack(){
        firstClickTimer = 0;
        if(!IsOnline()){
            if(isReady){
			    isReady = false;
			    ReadyPlayers--;
			    colorText.Text = "Choose Color";
			    colorBG.Visible = true;
                if(!Game.UsingMouse()){
                    colorCursor.Visible = true;
                }
                PlayerMenu.selectedColors.Remove(playerBallSprite.SelfModulate);
		    }else{ 
                Game.TotalPlayers--;
                Game.PlayerDatas.RemoveAt(Id-1);
			    PlayerMenu.ColorMenus.Remove(this);
                JoinedPlayers--;
			    int index = 1;
			    foreach(ColorMenu Menu in PlayerMenu.ColorMenus){
			    	Menu.Id = index;
			    	Menu.SetPosition();
                    index++;
			    }
                QueueFree();
            }
        }else{
            OnlineLobby.LobbySettingsMenu.InColorMenu = false;
            QueueFree();
        }
        SFX.Play("Back",1.125f);
    }

    private void UpdateCursorPosition(){
        if(Game.UsingMouse()) colorCursor.Visible = false;
        if(colorCursor.Position.X != -300 + (192f * selectionX)){
            colorCursor.Position = new Vector2(-300 + (192f * selectionX), colorCursor.Position.Y);
        }
        if(colorCursor.Position.Y != -200 + (200f * selectionY)){
            colorCursor.Position = new Vector2(colorCursor.Position.X,-200 + (200 * selectionY));
        }
    }

    protected override void UpdateSelectionVisual(){
        if(selectionX < 4 && selectionX >= 0 && selectionY < 3 && selectionY >= 0){
            SetPlayerColor(COLOR_ARRAY[selectionY, selectionX]);
        }   
    }

    private void SwitchProfile(){
        if(isReady) return; 

        string currentProfile = Game.PlayerDatas[Id-1].ControlProfileName;
        int index = ControlProfileManager.Profiles.IndexOf(currentProfile);

        if(index < ControlProfileManager.Profiles.Count - 1){
            index++;
        }else{
            index = 0;
        }
        string newProfile = ControlProfileManager.Profiles[index];
        Game.PlayerDatas[Id-1].ControlProfileName = newProfile;
        profileLabel.Text = "Profile: " + newProfile;

        SFX.Play("Move"); 
    }

    public void SetPosition(){
        switch(Game.TotalPlayers){
            case 1:
                Scale = Vector2.One;
                Position = new Vector2(0,300);
                break;
            case 2:
                Scale = Vector2.One;
                if(Id == 1) Position = new Vector2(-600,300);
                else Position = new Vector2(600,300);
                break;
            case 3:
                Scale = Vector2.One;
                switch (Id){
                    case 1: Position = new Vector2(-1000, 300); break;
                    case 2: Position = new Vector2(0, 300); break;
                    case 3: Position = new Vector2(1000, 300); break;
                }
                break;
            case 4:
                Scale = new Vector2(0.9f,0.9f);
                switch(Id){
                    case 1: Position = new Vector2(-1350,300); break;
                    case 2: Position = new Vector2(-450,300); break;
                    case 3: Position = new Vector2(450,300); break;
                    case 4: Position = new Vector2(1350,300); break;
                }
                break;
            case 5:
                Scale = new Vector2(0.7f,0.7f);
                switch(Id){
                    case 1: Position = new Vector2(-1000,-190); break;
                    case 2: Position = new Vector2(0,-190); break;
                    case 3: Position = new Vector2(1000,-190); break;
                    case 4: Position = new Vector2(-600,743); break;
                    case 5: Position = new Vector2(600,743); break;
                }
                break;
            case 6:
                Scale = new Vector2(0.7f,0.7f);
                switch(Id){
                    case 1: Position = new Vector2(-1000,-190); break;
                    case 2: Position = new Vector2(0,-190); break;
                    case 3: Position = new Vector2(1000,-190); break;
                    case 4: Position = new Vector2(-1000,743); break;
                    case 5: Position = new Vector2(0,743); break;
                    case 6: Position = new Vector2(1000,743); break;
                }
                break;
            case 7:
                Scale = new Vector2(0.7f,0.7f);
                switch (Id){
                    case 1: Position = new Vector2(-1350, -190); break;
                    case 2: Position = new Vector2(-450, -190); break;
                    case 3: Position = new Vector2(450, -190); break;
                    case 4: Position = new Vector2(1350, -190); break;
                    case 5: Position = new Vector2(-1000, 743); break;
                    case 6: Position = new Vector2(0, 743); break;
                    case 7: Position = new Vector2(1000, 743); break;
                }
                break;
            case 8:
                Scale = new Vector2(0.7f,0.7f);
                switch (Id){
                    case 1: Position = new Vector2(-1350, -190); break;
                    case 2: Position = new Vector2(-450, -190); break;
                    case 3: Position = new Vector2(450, -190); break;
                    case 4: Position = new Vector2(1350, -190); break;
                    case 5: Position = new Vector2(-1350, 743); break;
                    case 6: Position = new Vector2(-450, 743); break;
                    case 7: Position = new Vector2(450, 743); break;
                    case 8: Position = new Vector2(1350, 743); break;
                }
                break;
        }
    }
    
    private bool IsOnline(){
        return Game.GameNode.Multiplayer.MultiplayerPeer is not OfflineMultiplayerPeer && Game.GameNode.Multiplayer != null;
    }
    
    private void SetPlayerColor(Color color){
        playerBallSprite.SelfModulate = color;
        playerShadingSprite.SelfModulate = color;
    }
}