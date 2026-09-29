using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules
{
    public interface IPassif : IItemModule
    {
        public void OnCollide(Collision collision, ItemPickup item);
        public void OnThrow();
        public void OnUpdateState();
    }
}