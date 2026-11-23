namespace Gameplay.Items
{
    public interface IReplicatedModule
    {
        int GetState();
        void SetState(int state);
    }
    
    public interface IReplicatedByteModule
    {
        byte[] GetByteState();
        void SetByteState(byte[] data);
    }
}