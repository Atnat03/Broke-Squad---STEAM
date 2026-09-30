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
}