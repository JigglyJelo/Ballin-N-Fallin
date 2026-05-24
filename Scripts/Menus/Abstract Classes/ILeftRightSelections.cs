public interface ILeftRightSelections{
    public const float HOLD_SPEED_1_TIME = 0.25f; //How long to hold before it starts activating
	public const float HOLD_SPEED_2_TIME = HOLD_SPEED_1_TIME + 0.5f;
	public const float HOLD_SPEED_3_TIME = HOLD_SPEED_2_TIME + 0.5f;
	public const float HOLD_SPEED_4_TIME = HOLD_SPEED_3_TIME + 0.8f;
	public const float HOLD_UPDATE_SPEED_1 = 0.1f;
	public const float HOLD_UPDATE_SPEED_2 = 0.5f;
	public const float HOLD_UPDATE_SPEED_3 = 0.025f;
	public const float HOLD_UPDATE_SPEED_4 = 0.0125f;
    public static float HoldTimer = 0;
    public static float HoldUpdateTimer = 0;
    void MenuLeft();
    void MenuRight();

    public static bool HoldCheck(){
        if(HoldTimer == 0 || 
		(HoldTimer >= HOLD_SPEED_4_TIME && HoldUpdateTimer >= HOLD_UPDATE_SPEED_4) ||
		(HoldTimer >= HOLD_SPEED_3_TIME && HoldUpdateTimer >= HOLD_UPDATE_SPEED_3) ||
		(HoldTimer >= HOLD_SPEED_2_TIME && HoldUpdateTimer >= HOLD_UPDATE_SPEED_2) || 
		(HoldTimer >= HOLD_SPEED_1_TIME && HoldUpdateTimer >= HOLD_UPDATE_SPEED_1)){
            HoldUpdateTimer = 0;
            return true;
        }else{
            return false;
        }
    }

    public static void ResetHold(){
        HoldTimer = 0;
        HoldUpdateTimer = 0;
    }

    public static void UpdateHold(float delta){
        HoldTimer += delta;
        HoldUpdateTimer += delta;
    }
}