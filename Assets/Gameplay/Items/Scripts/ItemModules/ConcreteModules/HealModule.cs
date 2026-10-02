using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public class HealModule : ItemModule, ILeftClick
    {
        [SerializeField] private int _healAmount = 10;
        [SerializeField] private float _distanceToHeal = 2f;
        [SerializeField, SoundName] private string _healingSound;
        
        public float Range => _distanceToHeal;
        
        public void StartLeftClick()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _distanceToHeal)
                && hit.collider.GetComponentInParent<PlayerData.PlayerData>() is { } player
                && player.TryGetComponent(out NetworkObject netObj))
            {
                Context.Inventory.TryPlaySound(_healingSound);
                Context.Inventory.RequestHeal(netObj);
                Context.Core.UseCondition();
            }
        }

        public void EndLeftClick()
        { }
        
        public void ApplyHeal(PlayerData.PlayerData target) => target.Heal(_healAmount);
    }
}