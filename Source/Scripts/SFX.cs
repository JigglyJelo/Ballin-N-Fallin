using Godot;
using System.Collections.Generic;

public partial class SFX : Node{
	private static Dictionary<string, AudioStreamPlayer2D> sounds;
	public override void _Ready(){
		Game.DisableProcesses(this);
		sounds = new Dictionary<string, AudioStreamPlayer2D>();
		//Makes dictionary of all children sounds with their key as their name
		foreach(AudioStreamPlayer2D audio in GetChildren()){
			if(Level.LevelNode != null && Level.LevelNode.CameraZoom > 0){
				float zoomScale = 1 + (1-Level.LevelNode.CameraZoom);
				audio.MaxDistance = 2304*zoomScale;
			}
			string key = audio.Name;
			sounds.Add(key,audio);
		}
	}

	//Plays the sound effect by the Name of the SFX child node in Game Scene
	/// <summary>Plays a sound effect.</summary>
	/// <param name="sound">The exact name of the SFX child node.</param>
	/// <param name="pitch">The pitch multiplier. Defaults to 1.0.</param>
	public static void Play(string sound,float pitch = 1){
		sounds[sound].Position = Vector2.Zero;
		sounds[sound].PitchScale = pitch;
		sounds[sound].Play();
	}

	/// <summary>Plays a sound effect at a specific global position.</summary>
	/// <param name="sound">The exact name of the SFX child node.</param>
	/// <param name="position">The global position where the sound should originate.</param>
	public static void Play(string sound,Vector2 position){
		Play(sound,1,position);
	}

	/// <summary>Plays a sound effect at a specific global position with a modified pitch.</summary>
	/// <param name="sound">The exact name of the SFX child node.</param>
	/// <param name="pitch">The pitch multiplier.</param>
	/// <param name="position">The global position where the sound should originate.</param>
	public static void Play(string sound,float pitch, Vector2 position){
		AudioStreamPlayer2D soundEffect = sounds[sound];
		soundEffect.GlobalPosition = position;
		soundEffect.PitchScale = pitch;
		soundEffect.Play();
	}
}