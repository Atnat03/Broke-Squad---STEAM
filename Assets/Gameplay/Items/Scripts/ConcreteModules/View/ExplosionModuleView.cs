using Bus;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public class ExplosionModuleView : MonoBusListener
    {
        [SerializeField] private string _eventKey = "EXPLOSION_EVENT";
        
        private void OnEnable() => ListenToEvent<OnModuleDoAction_EVENT>(UpdateExplosion);

        private void UpdateExplosion(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _eventKey) return;
            if (data.HostId != GetHostId()) return;

            if (data.ValueB)
            {
                ABPrint.Print("Woow");
            }else
            {
                ABPrint.Print("Explosion");
            }
        }
        
        private ulong GetHostId()
        {
            ItemPickup pickup = GetComponentInParent<ItemPickup>();
            
            if (pickup != null) 
                return pickup.NetworkObjectId;

            ItemCore core = GetComponentInParent<ItemCore>();
            return core != null ? core.HostId : ulong.MaxValue;
        }
    }
}