using Unity.Netcode;

namespace Gameplay.Equipment
{
    public struct OnTryToPickUpBackPack_EVENT
    {
        public ulong ClientId;
        public NetworkObjectReference BackPackNOR;
    }
}