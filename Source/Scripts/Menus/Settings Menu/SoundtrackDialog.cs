using Godot;

public partial class SoundtrackDialog : FileDialog{

    public override void _Ready(){
        if(MenuScene.CurrentMenuNode is SoundMenu soundMenu){
            soundMenu.InMusicSelection = true;
        }
    }

    private void FolderChosen(string dir){
        Game.CustomSoundtrack = dir + "/";
		LoadMenuMusic();
        ClosedDialog();
    }

    private void LoadMenuMusic(){
		MenuScene.MenuNode.Music.Playing = false;
		AudioStream stream = MusicPlayer.GetCustomSong("Menu");
		if(stream != null) MenuScene.MenuNode.Music.Stream = stream;
		else MenuScene.MenuNode.Music.Stream = GD.Load<AudioStream>("res://Assets/Music/Menu.ogg");
		MenuScene.MenuNode.Music.Playing = true;
	}

    private void SaveSoundtrack(){
        Game.Save.SetValue("Sound","Custom Soundtrack",Game.CustomSoundtrack);
		Game.Save.Save(Game.SAVE_PATH);
    }

    private void ClosedDialog(){
        if(MenuScene.CurrentMenuNode is SoundMenu soundMenu){
            soundMenu.InMusicSelection = false;
        }
    }
}