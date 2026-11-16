using Bus;

namespace Gameplay.Items
{
    public class AwareItemView : MonoBusListener
    {
        protected ulong GetHostId()
        {
            ItemPickup pickup = GetComponentInParent<ItemPickup>();
            
            if (pickup != null) 
                return pickup.NetworkObjectId;

            ItemCore core = GetComponentInParent<ItemCore>();
            return core != null ? core.HostId : ulong.MaxValue;
        }
    }
}