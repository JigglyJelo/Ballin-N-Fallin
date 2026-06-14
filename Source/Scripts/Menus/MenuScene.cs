using Godot;
using System;

public partial class MenuScene : Node{
	public static MenuScene MenuNode;
	public AudioStreamPlayer Music;
	public const string MENU_PATH = "res://Source/Scenes/Object Scenes/Menus/";
	public static string MenuToLoad = "";
	public static Node CurrentMenuNode = null;
	public override void _Ready(){
		Game.DisableProcesses(this);
		GD.Print("Menu");
		MenuNode = this;
		Music = GetNode<AudioStreamPlayer>("Music");
		Music.Playing = false;
		if(Game.FirstBoot){
			Game.Save.Load(Game.SAVE_PATH);
			SettingsMenu.LoadData();
			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"),(float)(20 * Math.Log10(Game.MusicVolume / 100.0)));
			AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"),(float)(20 * Math.Log10(Game.SFXVolume / 100.0)));
			Game.FirstBoot = false;
			LoadMenu("MainMenu");
			Game.SetResolution();
		}else{
			Game.SetResolution();
			LoadMenu(!string.IsNullOrEmpty(MenuToLoad) ? MenuToLoad : "MainMenu");
		}
		GD.Print(Game.CustomSoundtrack);
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