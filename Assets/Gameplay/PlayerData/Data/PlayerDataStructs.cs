namespace Gameplay.PlayerData
{
    public struct PlayerDataEvent
    {
        public int playerHp;
        public int maxHp;
    }

    public struct PlayerDeathEvent
    {
        public ulong playerID;
    }

    public struct StaminaChangedEvent
    {
        public float stamina;
        public float maxStamina;
    }
    
    public struct PlayerRevivedEvent
    {
        public ulong playerID;
    }
    
    public struct PlayerStatusChangedEvent
    {
        public ulong playerID;
        public bool invincible;
    }
}