using Godot;

public partial class PlayerData{
    public string Username;
    public PlayerInputDevice InputDevice;
    public bool VibrationEnabled;
	public int UUID;
    public Color PlayerColor;

    public PlayerData(string username, PlayerInputDevice inputId, int uuid){
        Username = username;
        InputDevice = inputId;
        UUID = uuid;
        VibrationEnabled = true;
    }

    public PlayerData(string username, PlayerInputDevice inputId, int uuid, bool vibrationEnabled){
        Username = username;
        InputDevice = inputId;
        UUID = uuid;
        VibrationEnabled = vibrationEnabled;
    }

    public enum PlayerInputDevice : int{
        None = -1,
        Gamepad1 = 0,
        Gamepad2 = 1,
        Gamepad3 = 2,
        Gamepad4 = 3,
        Gamepad5 = 4,
        Gamepad6 = 5,
        Gamepad7 = 6,
        Gamepad8 = 7,
        Mouse = 8
    }
}