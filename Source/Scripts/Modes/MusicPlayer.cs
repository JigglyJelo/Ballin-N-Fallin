using Godot;
using System;
using System.Collections.Generic;

public partial class MusicPlayer : AudioStreamPlayer{
	public static MusicPlayer MusicNode;
	
	public override void _Ready(){
		MusicNode = this;
		if(Game.CurrentMode != Mode.GameMode.None && Game.CurrentMode != Mode.GameMode.Miscellaneous && string.IsNullOrEmpty(Game.CustomSoundtrack)){
			loadDefaultMusic();
		}else if(!string.IsNullOrEmpty(Game.CustomSoundtrack)){
			AudioStream stream = GetCustomSong(Mode.EnumToString(Game.CurrentMode));
			if(stream == null) loadDefaultMusic();
			else{
				Stream = stream;
				Play();
			} 
		}	

		void loadDefaultMusic(){
			if(Game.CurrentMode != Mode.GameMode.None && Game.CurrentMode != Mode.GameMode.Miscellaneous){
				Stream = GD.Load<AudioStream>("res://Assets/Music/Modes/" + Mode.EnumToString(Game.CurrentMode) + ".ogg");
				Play();
			}
				
			if(Game.CurrentMode == Mode.GameMode.HotPotato){
				HotPotatoSeek();
				GD.Print("Total" + Game.TotalPlayers);
			}
		}
	}

	public static void PauseMusic(bool paused){
		MusicNode.StreamPaused = paused;
	}

	public static void RoundOver(bool tourOver){
		if(tourOver) MusicNode.Stream = GD.Load<AudioStream>("res://Assets/Music/Victory.ogg");
		else MusicNode.Stream = GD.Load<AudioStream>("res://Assets/Music/Score.ogg");
		MusicNode.Play();
	}

	public static AudioStream GetCustomSong(string song){
		string filePath = Game.CustomSoundtrack+song;
		List<string> files = new List<string>();
		GD.Print(filePath);
		if(DirAccess.DirExistsAbsolute(filePath)){
			foreach(string file in DirAccess.GetFilesAt(filePath)){ //
				if(file.EndsWith(".ogg") || file.EndsWith(".mp3") || file.EndsWith(".wav")){
					files.Add(file);
					GD.Print(file);
				}
			}
			if(files.Count > 0){
				string fileNameAndPath = filePath + "/" + files[new Random().Next(0,files.Count)];
				GD.Print(fileNameAndPath);
				switch(fileNameAndPath.Substring(fileNameAndPath.Length - 4)){ //Do - 3 and remove . from switches
					case ".ogg":
						AudioStreamOggVorbis oggStream = AudioStreamOggVorbis.LoadFromFile(fileNameAndPath);
						oggStream.Loop = true;
						return oggStream;
					case ".wav": // This is somewhat broken but not going to fix it until I update to 4.4+ because it has built in support for .wav
       					FileAccess wavFile = FileAccess.Open(fileNameAndPath, FileAccess.ModeFlags.Read);
        				byte[] wavByteArray = wavFile.GetBuffer((int)wavFile.GetLength());
						AudioStreamWav wavStream = new AudioStreamWav();
						wavStream.MixRate = (wavByteArray[24] | (wavByteArray[25] << 8) | (wavByteArray[26] << 16) | (wavByteArray[27] << 24));
						GD.Print(wavStream.MixRate);
        				wavStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        				wavStream.Stereo = (wavByteArray[22] | (wavByteArray[23] << 8)) == 2;
        				wavStream.Data = wavByteArray;
						wavStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
						wavStream.LoopEnd = (int)(wavStream.MixRate * wavStream.GetLength());
        				return wavStream;
					case ".mp3":
						FileAccess mp3File = FileAccess.Open(fileNameAndPath, FileAccess.ModeFlags.Read);
        				byte[] mp3ByteArray = mp3File.GetBuffer((int)mp3File.GetLength());
						AudioStreamMP3 mp3Stream = new AudioStreamMP3();
        				mp3Stream.Data = mp3ByteArray;
						mp3Stream.Loop = true;
        				return mp3Stream;   
				}
			}
		}
		return null;
	}

	public static void HotPotatoSeek(){
		switch(Game.TotalPlayers){
			case 2: MusicNode.Seek(72); break;
			case 3: MusicNode.Seek(47); break;
			case 4: MusicNode.Seek(35); break;
			case 5: MusicNode.Seek(26); break;
			case 6: MusicNode.Seek(16); break;
			case 7: MusicNode.Seek(5); break;
		}
	}

	public static float GetPitch(){
		if(MusicNode != null) return MusicNode.PitchScale;
		else return 1;
	}

	public static void SetPitch(float pitch){
		if(MusicNode != null) MusicNode.PitchScale = pitch;
		float audioBusPitch;
		int musicBusIndex = AudioServer.GetBusIndex("Music");
		AudioEffectPitchShift pitchShift = AudioServer.GetBusEffect(musicBusIndex,0) as AudioEffectPitchShift;
		AudioServer.SetBusEffectEnabled(musicBusIndex,0,Game.CurrentScene == Game.SceneType.Game);
		if(pitch != 1){
			audioBusPitch = 1 / pitch;
			pitchShift.PitchScale = audioBusPitch;
		}else{
			pitchShift.PitchScale = 1;
		}
	}
}