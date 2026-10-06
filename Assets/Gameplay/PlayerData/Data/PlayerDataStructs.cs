namespace Gameplay.PlayerData
{
    public struct PlayerDataEvent
    {
        public int playerHp;
        public int maxHp;
    }
    public struct StaminaChangedEvent
    {
        public float stamina;
        public float maxStamina;
    }
}