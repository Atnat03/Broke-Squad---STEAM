namespace Gameplay.PlayerData
{
    public struct PlayerHealthChanged_EVENT
    {
        public int CurrentHp;
        public int MaxHp;
        public bool Invincible;
    }
    public struct StaminaChanged_EVENT
    {
        public float Current;
        public float MaxStamina;
    }
}