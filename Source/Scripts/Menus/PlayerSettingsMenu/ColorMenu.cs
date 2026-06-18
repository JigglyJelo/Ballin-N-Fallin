using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ColorMenu : Menu2D{
    private PlayerSettingsMenu parent;
    private int selectionX = 0;
    private int selectionY = 0;

    private Polygon2D colorBG;
    private Sprite2D colorCursor;
    private Label colorText;
    private Sprite2D[,] colorButtons = new Sprite2D[3,4];

    private float firstClickTimer = 0;
    private const float FIRST_CLICK_TIMEOUT = 0.25f;

    public static List<Color> DefaultColorOrder = new List<Color>{
        Game.Colors["Orange"], Game.Colors["Lime"], Game.Colors["Cyan"], Game.Colors["Pink"],
        Game.Colors["Yellow"], Game.Colors["White"], Game.Colors["Gray"], Game.Colors["Brown"], Game.Colors["Red"]
    };
    
    private readonly Color[,] COLOR_ARRAY = new Color[3, 4]{
        {Game.Colors["Orange"], Game.Colors["Lime"], Game.Colors["Cyan"], Game.Colors["Pink"]},
        {Game.Colors["Red"], Game.Colors["Green"], Game.Colors["Blue"], Game.Colors["Purple"]},
        {Game.Colors["Yellow"], Game.Colors["White"], Game.Colors["Gray"], Game.Colors["Brown"]}
    };

    public void Init(PlayerSettingsMenu parentMenu){
        parent = parentMenu;
        colorBG = GetNode<Polygon2D>("ColorBackground");
        colorCursor = GetNode<Sprite2D>("ColorBackground/ColorSelector");
        colorText = GetNode<Label>("Color Text");

        if(!Game.UsingMouse()){
            for(int i = 0; i < COLOR_ARRAY.GetLength(0); i++){
                for(int j = 0; j < COLOR_ARRAY.GetLength(1);j++){
                    Color? playerColor;
                    if(Online.IsOnline){
                        playerColor = Online.GetClientsPlayerData()?.PlayerColor;
                    }else{
                        playerColor = Game.GetPlayerColor(parent.Id);
                    }
                    if(COLOR_ARRAY[i,j].Equals(playerColor)){
                        selectionX = j;
                        selectionY = i;
                        UpdateCursorPosition();
                        break;
                    }
                }
            }
        }else{ 
            colorCursor.Visible = false;
            int index = 0;
            string[] keys = Game.Colors.Keys.ToArray();
            for(int i = 0; i < colorButtons.GetLength(0); i++){
                for(int j = 0; j < colorButtons.GetLength(1); j++){
                    colorButtons[i, j] = GetNode<Sprite2D>("ColorBackground/" + keys[index++]);
                }
            }
        }
    }

    public override void _Process(double delta){
        if(!Visible) return;

        if(!Game.UsingMouse() && parent.InputId != (int)PlayerData.PlayerInputDevice.Mouse){
            InputChecks(delta, parent.InputId);
        }else{
            if(!parent.isReady){
                firstClickTimer += (float)delta;
                foreach(Sprite2D colorButton in colorButtons){
                    Vector2 buttonPosition = colorButton.GlobalPosition;
                    if(Mathf.Abs(buttonPosition.DistanceTo(GetGlobalMousePosition())) < 90){
                        Cursor.CursorThisFrame = Input.CursorShape.PointingHand;
                        if(parent.playerBallSprite.SelfModulate != Game.Colors[colorButton.Name]){
                            SFX.Play("Move");
                            parent.SetPlayerColor(Game.Colors[colorButton.Name]);
                        }
                        
                        if(Input.IsActionJustReleased("Charge N Launch Mouse") && firstClickTimer > FIRST_CLICK_TIMEOUT){
                            MenuChoose(0); 
                        }
                    }
                }
            }
            if(Input.IsActionJustReleased("Slam Mouse")) MenuBack();
        }
    }

    protected override void MenuLeft(){
        if(parent.isReady) return;
        selectionX = (selectionX > 0) ? selectionX - 1 : 3;
        UpdateCursorPosition();
        UpdateSelectionVisual();
    }

    protected override void MenuRight(){
        if(parent.isReady) return;
        selectionX = (selectionX < 3) ? selectionX + 1 : 0;
        UpdateCursorPosition();
        UpdateSelectionVisual();
    }

    protected override void MenuDown(){
        if(parent.isReady) return;
        selectionY = (selectionY < 2) ? selectionY + 1 : 0;
        UpdateCursorPosition();
        UpdateSelectionVisual();
    }

    protected override void MenuUp(){
        if(parent.isReady) return;
        selectionY = (selectionY > 0) ? selectionY - 1 : 2;
        UpdateCursorPosition();
        UpdateSelectionVisual();
    }

    protected override void MenuChoose(int selection){
        if(!Online.IsOnline){
            if(!parent.isReady && !PlayerMenu.selectedColors.Contains(parent.playerBallSprite.SelfModulate)){
                Game.PlayerDatas[parent.Id-1].PlayerColor = parent.playerBallSprite.SelfModulate;
                PlayerMenu.selectedColors.Add(parent.playerBallSprite.SelfModulate);
                colorText.Text = "Ready";
                colorBG.Visible = false;
                colorCursor.Visible = false;
                
                PlayerSettingsMenu.ReadyPlayers++;
                parent.isReady = true;
                SFX.Play("Confirm",1.125f);
            }
        }else{
            if(Game.UsingMouse()){
                OnlineLobby.Lobby.RpcId(1,nameof(OnlineLobby.Lobby.SwitchColor),parent.playerBallSprite.SelfModulate);
            }else{
                OnlineLobby.Lobby.RpcId(1,nameof(OnlineLobby.Lobby.SwitchColor),COLOR_ARRAY[selectionY, selectionX]);
            }
            SFX.Play("Confirm",1.125f);
            MenuBack();
        }
    }

    public override void MenuBack(){
        firstClickTimer = 0;
        if(!Online.IsOnline){
            if(parent.isReady){
                parent.isReady = false;
                PlayerSettingsMenu.ReadyPlayers--;
                colorText.Text = "Choose Color";
                colorBG.Visible = true;
                if(!Game.UsingMouse()){
                    colorCursor.Visible = true;
                }
                PlayerMenu.selectedColors.Remove(parent.playerBallSprite.SelfModulate);
            }else{ 
                Game.TotalPlayers--;
                Game.PlayerDatas.RemoveAt(parent.Id-1);
                PlayerMenu.SettingMenus.Remove(parent); 
                PlayerSettingsMenu.JoinedPlayers--;
                
                int index = 1;
                foreach(PlayerSettingsMenu Menu in PlayerMenu.SettingMenus){
                    Menu.Id = index;
                    Menu.SetPosition();
                    index++;
                }
                parent.QueueFree(); 
            }
        }else{
            OnlineLobby.LobbySettingsMenu.InColorMenu = false;
            parent.QueueFree();
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
            parent.SetPlayerColor(COLOR_ARRAY[selectionY, selectionX]);
        }   
    }
}