using Godot;

public partial class SettingsOnlineMenu : VerticalMenu, ILeftRightSelections{
	public static ChatSetting OnlineChatSetting;
	public static bool UsePlayerPrediction;
	private Label chatText, predictionText;
	private LineEdit norayEntry, norayPortEntry, nohubEntry, nohubPortEntry;
	private const string DEFAULT_NORAY_IP = "127.0.0.1";
	private const ushort DEFAULT_NORAY_PORT = 8890;
	private const string DEFAULT_NOHUB_IP = "foxssake.studio";
	private const ushort DEFAULT_NOHUB_PORT = 12980;
	public override void _Ready(){
		base._Ready();
		Selection = 1;
		totalSelections = 2;
		defaultFontSize = 1;
		LoadData();
		chatText = GetNode<Label>("Selections/Chat Text");
		predictionText = GetNode<Label>("Selections/Prediction Text");
		norayEntry = GetNode<LineEdit>("NorayIPEntry");
		norayPortEntry = GetNode<LineEdit>("NorayPortEntry");
		nohubEntry = GetNode<LineEdit>("NohubIPEntry");
		nohubPortEntry = GetNode<LineEdit>("NohubPortEntry");
		LoadData();
		UpdateTexts();
		UpdateSelectionVisual();
	}

	public override void _Process(double delta){
		base._Process(delta);
		for(int i = 0; i < Game.MAX_PLAYERS; i++){
			if(Input.IsActionJustReleased("Y"+i)){
				SetToDefaultSettings();
			}
		}
	}

	protected override void MenuChoose(int choice){
		if(choice == 2) UsePlayerPrediction = !UsePlayerPrediction;
	}

	public override void MenuBack(){
		SFX.Play("Back");
		SaveData();
		MenuScene.LoadMenu("Settings/SettingsMenu");
	}

	public void MenuRight(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		switch(Selection){
			case 1: 
				OnlineChatSetting++;
				if(OnlineChatSetting > ChatSetting.Filtered) OnlineChatSetting = ChatSetting.Disabled;
				break;
			case 2: 
				UsePlayerPrediction = !UsePlayerPrediction;
				break;
		}
		joystickTimer = 0;
		UpdateTexts();
	}

	public void MenuLeft(){
		SFX.Play("Move",Game.Random.Next(80,110)/100f);
		switch(Selection){
			case 1: 
				OnlineChatSetting--;
				if(OnlineChatSetting < ChatSetting.Disabled) OnlineChatSetting = ChatSetting.Filtered;
				break;
			case 2: 
				UsePlayerPrediction = !UsePlayerPrediction;
				break;
		}
		joystickTimer = 0;
		UpdateTexts();
	}

	private void UpdateTexts(){
		switch(OnlineChatSetting){
			case ChatSetting.Disabled:
				chatText.Text = "Chat: Disabled";
				break;
			case ChatSetting.Enabled:
				chatText.Text = "Chat: Enabled";
				break;
			case ChatSetting.Filtered:
				chatText.Text = "Chat: Filtered";
				break;
		}
		predictionText.Text = UsePlayerPrediction ? "Prediction: Enabled" : "Prediction: Disabled";
		norayEntry.Text = NoraySetup.NorayIP;
		norayPortEntry.Text = NoraySetup.NorayPort.ToString();
		nohubEntry.Text = NohubHostManager.NohubIP;
		nohubPortEntry.Text = NohubHostManager.NohubPort.ToString();
		SaveData();
		LoadData();
	}

	private void SetToDefaultSettings(){
		OnlineChatSetting = ChatSetting.Filtered;
		UsePlayerPrediction = true;
		NoraySetup.NorayIP = DEFAULT_NORAY_IP;
		NoraySetup.NorayPort = DEFAULT_NORAY_PORT;
		NohubHostManager.NohubIP = DEFAULT_NOHUB_IP;
		NohubHostManager.NohubPort = DEFAULT_NOHUB_PORT;
		UpdateTexts();
	}

	private void SaveData(){
		NoraySetup.NorayIP = norayEntry.Text;
		try{
			NoraySetup.NorayPort = ushort.Parse(norayPortEntry.Text);
		}catch{}
		NohubHostManager.NohubIP = nohubEntry.Text;
		try{
			NohubHostManager.NohubPort = ushort.Parse(nohubPortEntry.Text);
		}catch{}
		Game.Save.SetValue("Online","Chat", (int)OnlineChatSetting);
		Game.Save.SetValue("Online","Prediction", UsePlayerPrediction);
		Game.Save.SetValue("Online","noray IP", NoraySetup.NorayIP);
		Game.Save.SetValue("Online","noray Port", NoraySetup.NorayPort);
		Game.Save.SetValue("Online","nohub IP", NohubHostManager.NohubIP);
		Game.Save.SetValue("Online","nohub Port", NohubHostManager.NohubPort);
		Game.Save.Save(Game.SAVE_PATH);
	}

	public static void LoadData(){
		Game.Save.Load(Game.SAVE_PATH);
		//Volume
		OnlineChatSetting = (ChatSetting)(int)Game.Save.GetValue("Online", "Chat", (int)ChatSetting.Filtered);
		UsePlayerPrediction = (bool)Game.Save.GetValue("Online", "Prediction", true);
		NoraySetup.NorayIP = (string)Game.Save.GetValue("Online", "noray IP", DEFAULT_NORAY_IP);
		NoraySetup.NorayPort = (ushort)Game.Save.GetValue("Online", "noray Port", DEFAULT_NORAY_PORT);
		NohubHostManager.NohubIP = (string)Game.Save.GetValue("Online", "nohub IP", DEFAULT_NOHUB_IP);
		NohubHostManager.NohubPort = (ushort)Game.Save.GetValue("Online", "nohub Port", DEFAULT_NOHUB_PORT);
	}

	public enum ChatSetting : int{
		Disabled, Enabled, Filtered
	}
}
