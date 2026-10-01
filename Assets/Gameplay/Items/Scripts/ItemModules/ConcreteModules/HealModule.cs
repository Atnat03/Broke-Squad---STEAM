using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    public class HealModule : ItemModule, ILeftClick
    {
        [SerializeField] private int _healAmount = 10;
        [SerializeField] private float _distanceToHeal = 2f;
        
        public void StartLeftClick()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _distanceToHeal)
                && hit.transform.TryGetComponent(out PlayerData.PlayerData player))
            {
                player.Heal(_healAmount);
            }
        }

        public void EndLeftClick()
        { }
    }
}