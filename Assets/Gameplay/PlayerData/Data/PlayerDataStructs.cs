namespace Gameplay.PlayerData
{
    public struct PlayerHealthChanged_EVENT
    {
        public float CurrentHp;
        public float MaxHp;
        public bool Invincible;
    }
    public struct StaminaChanged_EVENT
    {
        public float Current;
        public float MaxStamina;
    }

    public struct DownedChanged_EVENT
    {
        public bool Downed;
        public ulong PlayerId;
    }
}