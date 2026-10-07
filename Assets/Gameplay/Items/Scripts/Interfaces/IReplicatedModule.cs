namespace Gameplay.Items
{
    public interface IReplicatedModule
    {
        int GetState();
        void SetState(int state);
    }
}