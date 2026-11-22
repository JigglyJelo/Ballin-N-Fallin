using Godot;

public partial class MenuScene : Node{
	public static MenuScene MenuNode;
	public AudioStreamPlayer Music;
	public const string MENU_PATH = "res://Source/Scenes/Menus/";
	public static string MenuToLoad = "";
	public static Node CurrentMenuNode = null;
	public override void _Ready(){
		Game.DisableProcesses(this);
		MenuNode = this;
		Music = GetNode<AudioStreamPlayer>("Music");
		Music.Playing = false;
		if(Game.FirstBoot){
			SaveManager.LoadSaveData();
			Game.FirstBoot = false;
			LoadMenu("MainMenu");
		}else{
			LoadMenu(!string.IsNullOrEmpty(MenuToLoad) ? MenuToLoad : "MainMenu");
		}
		Game.SetResolution();
		AudioStream stream = MusicPlayer.GetCustomSong("Menu");
		if(stream != null) Music.Stream = stream;
		else Music.Stream = GD.Load<AudioStream>("res://Assets/Music/Menu.ogg");
		Music.Playing = true;
		MenuToLoad = "";
	}

	public static void SetCamera(){
		if(MenuNode == null) Game.GameNode.GetNode<MenuScene>("Scene");
		Window window = Game.GameNode.GetTree().Root;
		Game.Camera.Zoom = new Vector2(window.ContentScaleSize.Y / 2160f,window.ContentScaleSize.Y / 2160f);
	}

	public static void MenuBackgroundFadeout(){
		CanvasLayer backgroundLayer = new CanvasLayer();
		MenuBackground background = MenuNode.GetNode<MenuBackground>("Background");
		background.GlobalPosition = new Vector2(1920,1080);
		backgroundLayer.Name = "BackgroundLayer";
		Game.GameNode.AddChild(backgroundLayer);
		background.Reparent(backgroundLayer);
	}

	public static void LoadMenu(string menuToLoad){
		Node oldNode = CurrentMenuNode;
		CurrentMenuNode = GD.Load<PackedScene>(MENU_PATH + menuToLoad + ".tscn").Instantiate<Node>();
		MenuNode.AddChild(CurrentMenuNode);
		if(oldNode != null && !oldNode.IsQueuedForDeletion()){
			oldNode.QueueFree();
		}
	}
}