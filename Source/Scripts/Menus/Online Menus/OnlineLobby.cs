using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public partial class OnlineLobby : Node{
    public bool IsHost;
    private Label connectedPlayersText;
    private OnlinePlayerText[] playerTexts = new OnlinePlayerText[Game.MAX_PLAYERS];
    public static OnlineLobby Lobby;
    public static LobbySettingsMenu LobbySettingsMenu;
    public bool LobbyShown = true;
    private float startTimer, unableToJoinLobbyTimer;
    private const float CONNECT_TIMEOUT = 10;
    private bool starting = false;
    private static Gradient pingGradient = GD.Load<Gradient>("res://Assets/Gradients/Ping.tres");
    private Sprite2D banButton;
    public long LobbyID = -1;

    public override void _Ready(){
        Lobby = this;
        if(!Online.IsOnline) Game.PlayerDatas = new List<PlayerData>();
        else if(IsHost){
            Game.PlayerDatas = new List<PlayerData>();
            PlayerData hostData = new PlayerData(Online.Username, Online.InputId, 1);
            hostData.PlayerColor = ColorMenu.DefaultColorOrder[0]; // Give host the first color
            Game.PlayerDatas.Add(hostData);
        }
        WordFilter.Initialize();
        Game.GameNode.Multiplayer.PeerConnected += PeerConnected;
        Game.GameNode.Multiplayer.PeerDisconnected += PeerDisconnected;
        Game.GameNode.Multiplayer.ConnectedToServer += ConnectedToServer;
        PackedScene playerTextScene = GD.Load<PackedScene>("res://Source/Scenes/Menus/Online/OnlinePlayerText.tscn");
        for(int i = 0; i < playerTexts.Length; i++){
            OnlinePlayerText playerText = playerTextScene.Instantiate<OnlinePlayerText>();
            playerText.Name = "Player " + (i+1) + " Text";
            playerText.Position = new Vector2(450,(200*i) - 750);
            GetNode("PlayerTexts").AddChild(playerText);
            playerTexts[i] = playerText;
            playerText.Id = i;
            playerText.Visible = false;
        }
        
        GD.Print(Online.IsOnline);
        if(!Online.IsOnline){
            if(IsHost) HostLobby();
            else JoinLobby();
        }else if(Online.IsHost()){
            if(NohubHostManager.IsNohubSetup()) NohubHostManager.UnlockLobby();
            Multiplayer.MultiplayerPeer.RefuseNewConnections = false;
        }
        
        if(Online.Network != Online.NetworkType.Steam || (Online.Network == Online.NetworkType.Steam && Online.IsOnline)){
            JoinedLobby();
        }
    }

    public override void _Process(double delta){
        if(starting){
            startTimer += (float)delta;
            if(startTimer >= 1){
                MenuScene.MenuBackgroundFadeout();
                GetTree().Paused = true;
                SceneTransitioner.SwitchToScene(Game.SceneType.Game);
            }
        }
    }

    public override void _PhysicsProcess(double delta){
        if(Online.IsHost() && Online.IsOnlinePeer() && Online.PeerIsActive()){
            KickBanPlayerButtons();
        }
        if(!Online.IsHost() && !Online.PeerIsActive()){
            unableToJoinLobbyTimer += (float)delta;
            if(unableToJoinLobbyTimer >= CONNECT_TIMEOUT){
                Online.Disconnect("Failed to connect to lobby");
            }
        }
    }

    private void KickBanPlayerButtons(){
        Vector2 globalMousePosition = banButton.GetGlobalMousePosition();
        if(globalMousePosition.X > 192 + 450 && globalMousePosition.X < 1920){
            for(int i = 0; i < Game.PlayerDatas.Count; i++){
                if(globalMousePosition.Y > playerTexts[i].GlobalPosition.Y && globalMousePosition.Y < playerTexts[i].GlobalPosition.Y + 128){
                    banButton.GlobalPosition = new Vector2(banButton.GlobalPosition.X, playerTexts[i].GlobalPosition.Y);
                    banButton.Visible = true;
                    if(globalMousePosition.X > banButton.GlobalPosition.X && globalMousePosition.X < banButton.GlobalPosition.X + 128){
                        banButton.SelfModulate = new Color(0.8f,0,0);
                        if(Input.IsActionJustReleased("Charge N Launch Mouse")){
                            int banUUID = Game.PlayerDatas[i].UUID;
                            GD.Print("Banned " + Game.PlayerDatas[i].Username);
                            Online.BanPlayer(banUUID);
                            Rpc(nameof(UpdatePlayerTexts));
                        }else if(Input.IsActionJustReleased("Slam Mouse")){
                            int kickUUID = Game.PlayerDatas[i].UUID;
                            GD.Print("Kicked " + Game.PlayerDatas[i].Username);
                            Online.KickPlayer(kickUUID);
                            Rpc(nameof(UpdatePlayerTexts));
                        }
                    }else{
                        banButton.SelfModulate = new Color(1,0,0);
                    }
                    return;
                }
            }
        }
        banButton.Visible = false;
    }

    private void HostLobby() {
        PingGetter.LastPing = 0;
        PingGetter.Pings[0] = 0;
        GD.Print("Host");
        
        if (!Game.IsDedicatedServer && (Online.Network == Online.NetworkType.Direct || Online.Network == Online.NetworkType.Noray)) {
            SendPlayerInfoDirect(Online.Username, (byte)Online.InputId, 1);
        }

        switch (Online.Network) {
            case Online.NetworkType.Direct:
                if (EnetSetup.EnetHost()) InitializeSuccessfulHost();
                else FailHostSetup();
                break;
            case Online.NetworkType.Noray:
                if (NoraySetup.NorayHost()) InitializeSuccessfulHost();
                else FailHostSetup();
                break;
            case Online.NetworkType.Steam:
                Node steamSetupNode = GD.Load<PackedScene>("res://Source/Scenes/Menus/Online/SteamMultiplayerPeerSetup.tscn").Instantiate();
                AddChild(steamSetupNode);
                steamSetupNode.Call("host_lobby");
                CreatePingGetter();
                break;
        }
        void InitializeSuccessfulHost() {
            Online.IsOnline = true;
            Online.BannedIps = new List<string>();
            CreatePingGetter();
        }

        void FailHostSetup() {
            Online.IsOnline = false;
            Game.PlayerDatas = new List<PlayerData>();
        }
    }

    public void JoinedLobby(){
        AddChild(GD.Load<PackedScene>("res://Source/Scenes/Menus/Online/LobbySettingsMenu.tscn").Instantiate<LobbySettingsMenu>());
        LobbySettingsMenu = GetNode<LobbySettingsMenu>("LobbySettingsMenu");
        banButton = GetNode<Sprite2D>("PlayerTexts/HostPlayerOptions/BanButton");
        UpdatePlayerTexts();
    }

    public void FailedToConnectSteamLobby(MultiplayerPeer peer, int condition){
        GD.Print("Steam Lobby Fail: " + condition);
        Online.FailedToStart(peer,Error.PrinterOnFire);
    }

    private void JoinLobby() {
        bool joinSuccess = false;
        
        switch (Online.Network) {
            case Online.NetworkType.Direct:
                joinSuccess = EnetSetup.EnetJoin();
                break;
            case Online.NetworkType.Noray:
                joinSuccess = NoraySetup.NorayJoin();
                break;
            case Online.NetworkType.Steam:
                Node steamSetupNode = GD.Load<PackedScene>("res://Source/Scenes/Menus/Online/SteamMultiplayerPeerSetup.tscn").Instantiate();
                AddChild(steamSetupNode);
                steamSetupNode.Call("join_lobby", LobbyID);
                return;
        }

        if(joinSuccess){
            Online.IsOnline = true;
        }else{
            Online.IsOnline = false;
            SceneTransitioner.SwitchToScene(Game.SceneType.Menu);
        }
    }

    private void ConnectedToServer(){
        GD.Print("Connected");
        if(Online.Network == Online.NetworkType.Direct || Online.Network == Online.NetworkType.Noray) {
            RpcId(1, nameof(SendPlayerInfoDirect), Online.Username, (byte)Online.InputId, Game.GameNode.Multiplayer.GetUniqueId());
        }
        CreatePingGetter();
    }

    private void PeerConnected(long id) {
        if (Online.IsHost() && id != 1) {
            if (Online.BannedIps.Contains(Online.GetIp((int)id))) {
                Game.GameNode.Multiplayer.MultiplayerPeer.DisconnectPeer((int)id);
                return;
            }
            
            Rpc(nameof(UpdateLobbySettings), Tour.IsTour, Tour.CurrentTour.PointsToWin, Tour.CurrentTour.ItemsEnabled, (byte)Game.StompSetting, Online.Buffer);
            
            Color playerColor = Colors.White;
            foreach (Color color in ColorMenu.DefaultColorOrder) {
                if (!Game.PlayerDatas.Any(player => player.PlayerColor == color)) {
                    playerColor = color;
                    break;
                }
            }
        }
    }

    private void PeerDisconnected(long id){
        if(Online.IsHost()) Rpc(nameof(UpdatePlayerTexts));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SendPlayerInfoDirect(string username,byte inputId, int uuid){
        if(Online.Network == Online.NetworkType.Direct || Online.Network == Online.NetworkType.Noray){
            if(Online.IsHost() && GetTree().GetMultiplayer().GetRemoteSenderId() != 0){
                uuid = GetTree().GetMultiplayer().GetRemoteSenderId();
            }
            if(Online.IsHost() || GetTree().GetMultiplayer().GetRemoteSenderId() == 0){
                username = Regex.Replace(username, "[^a-zA-Z0-9 ]", "");
                username = BadNameCheck(username,uuid);
                PlayerData playerInfo = new PlayerData(username,(PlayerData.PlayerInputDevice)inputId,uuid);
                if(!Game.PlayerDatas.Contains(playerInfo)){
                    foreach(Color color in ColorMenu.DefaultColorOrder){
                        if(!Game.PlayerDatas.Any(p => p.PlayerColor == color)){
                            playerInfo.PlayerColor = color;
                            break;
                        }
                    }
                    Game.PlayerDatas.Add(playerInfo);
                    Game.TotalPlayers = Game.PlayerDatas.Count;
                }
            }

            if(Game.GameNode.Multiplayer.IsServer()){
                string[] usernames = new string[Game.PlayerDatas.Count];
                byte[] inputIds = new byte[Game.PlayerDatas.Count];
                int[] uuids = new int[Game.PlayerDatas.Count];
                Color[] colors = new Color[Game.PlayerDatas.Count];
                for(int i = 0; i < Game.PlayerDatas.Count; i++){
                    usernames[i] = Game.PlayerDatas[i].Username;
                    inputIds[i] = (byte)Game.PlayerDatas[i].InputDevice;
                    uuids[i] = Game.PlayerDatas[i].UUID;
                    colors[i] = Game.PlayerDatas[i].PlayerColor;
                }
                Rpc(nameof(SyncPlayerInfosDirect),usernames,inputIds,uuids,colors);
            }
            UpdatePlayerTexts();
        }else{
            GD.PrintErr("Wrong SendPlayerInfo Rpc is being called this is the Direct function");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncPlayerInfosDirect(string[] usernames, byte[] inputIds, int[] uuids, Color[] colors){
        if(Online.Network == Online.NetworkType.Direct || Online.Network == Online.NetworkType.Noray){
            Game.PlayerDatas = new List<PlayerData>();
            for(int i = 0; i < inputIds.Length; i++){
                PlayerData playerInfo = new PlayerData(usernames[i],(PlayerData.PlayerInputDevice)inputIds[i],uuids[i]);
                playerInfo.PlayerColor = colors[i];
                Game.PlayerDatas.Add(playerInfo);
            }
            UpdatePlayerTexts();
        }
    }

    private string BadNameCheck(string username,int uuid){
        if(WordFilter.IsBadString(username)){
            GD.Print(username);
            username = getTrollName();
        }
        return username;
        
        string getTrollName(){
            switch(uuid){
                case 1: 
                    return WordFilter.trolledWords[Game.Random.Next(0,WordFilter.trolledWords.Count)];
                default:
                    int bitsToKeep = (int)MathF.Ceiling(MathF.Log2(WordFilter.trolledWords.Count));
                    int mask = (1 << bitsToKeep) - 1;
                    int trimmedUuid = uuid & mask;
                    int index = trimmedUuid % WordFilter.trolledWords.Count;
                    return WordFilter.trolledWords[index];
            }
        }
    }

    private void ShutdownLobby(){
        GD.PrintErr("Host Disconnected");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void UpdatePlayerTexts(){
        for(int i = 0; i < playerTexts.Length; i++){
            OnlinePlayerText playerText = playerTexts[i];
            try{
                playerText.UUID = Game.PlayerDatas[i].UUID;
            }catch{
                playerText.UUID = 0;
            }
            playerText.ResetPlayerText();
        }
        if(Online.IsHost() && NohubHostManager.IsNohubSetup()){
            NohubHostManager.UpdateLobbyData();
        }
    }

    public static Color GetPingTextColor(int ping){
        return pingGradient.Sample((float)ping/500);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = false,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncPlayerColors(Color[] playerColors){
        for(int i = 0; i < playerColors.Length; i++){
            Game.PlayerDatas[i].PlayerColor = playerColors[i];
        }
        UpdatePlayerTexts();
    }

    public static void ShowLobby(bool isShown){
        Lobby.LobbyShown = isShown;
        LobbySettingsMenu.Visible = isShown;
        foreach(Node node in LobbySettingsMenu.GetNode("Selections").GetChildren()){
            if(node is Label label){
                label.Visible = isShown;
            }
        }
        Lobby.GetNode<Node2D>("PlayerTexts").Visible = isShown;
        if(Game.UsingMouse()) Lobby.GetNode<Node2D>("LobbySettingsMenu/MenuBackButton").Visible = isShown;
    }

    private void CreatePingGetter(){
        if(Game.GameNode.GetNodeOrNull("PingGetter") == null){
            PingGetter pingNode = GD.Load<PackedScene>("res://Source/Scenes/Menus/Online/PingGetter.tscn").Instantiate<PingGetter>();
            Game.GameNode.AddChild(pingNode);
            Game.GameNode.MoveChild(pingNode,0);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void UpdateLobbySettings(bool isTour,int points, bool itemsOn,byte stompSetting, float buffer){
        Tour.IsTour = isTour;
        Tour.CurrentTour.PointsToWin = points;
        Tour.CurrentTour.ItemsEnabled = itemsOn;
        Game.StompSetting = (Game.StompSettingEnum)stompSetting;
        Online.Buffer = buffer;
        if(LobbySettingsMenu != null) LobbySettingsMenu.UpdateTexts();
        if(Online.IsHost() && NohubHostManager.IsNohubSetup()) NohubHostManager.UpdateLobbyData();
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartingGame(Mode.GameMode mode,string level,string folderPath){
        GD.Print("Start");
        Online.IsOnline = true;
        Tour.ResetPlayerScores();
        Game.TotalPlayers = Game.PlayerDatas.Count;
        Game.CurrentMode = mode;
        Game.SetLevel(Game.CurrentMode,level,folderPath);
        starting = true;
    }

    public void StartGame(){
        Game.GameNode.Multiplayer.MultiplayerPeer.RefuseNewConnections = true;
        if(NohubHostManager.IsNohubSetup()) NohubHostManager.LockLobby();
        Rpc(nameof(UpdateLobbySettings),Tour.IsTour,Tour.CurrentTour.PointsToWin,Tour.CurrentTour.ItemsEnabled,(byte)Game.StompSetting,Online.Buffer);
        Rpc(nameof(StartingGame),(byte)Game.CurrentMode,Game.CurrentLevelName,Game.CurrentFolderPath);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SwitchColor(Color color){
        if(Online.IsHost()){
            int senderId = GetTree().GetMultiplayer().GetRemoteSenderId();
            for(int i = 0; i < Game.PlayerDatas.Count; i++){
                if(Game.PlayerDatas[i].UUID == senderId){
                    for(int j = 0; j < Game.PlayerDatas.Count; j++){
                        if(color.Equals(Game.PlayerDatas[j].PlayerColor)){
                            return;
                        }
                    }
                    Game.PlayerDatas[i].PlayerColor = color;
                    Color[] playerColors = new Color[Game.PlayerDatas.Count];
                    for(int j = 0; j < playerColors.Length; j++){
                        playerColors[j] = Game.PlayerDatas[j].PlayerColor;
                    }
                    Rpc(nameof(SyncPlayerColors),playerColors);
                    UpdatePlayerTexts();
                    break;
                }
            }
        }else{
            GD.PrintErr(OnlineErrorMessages.NonHostCallErrorMessage());
        }
    }
}