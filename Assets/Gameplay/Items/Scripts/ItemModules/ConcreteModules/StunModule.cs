using System;
using Gameplay.IA.Scripts;
using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class StunModule : ItemModule, IPassif
    {
        [SerializeField] private float _minVelocityForStun = 2;
        [SerializeField] private float _stunDuration = 1f;
        
        public void OnCollide(Collision collision, ItemPickup item)
        {
            if (collision.gameObject.TryGetComponent<IStunnable>(out var stun))
            {
                stun.ApplyStun(_stunDuration);
            }
        }

        public void OnThrow()
        { }

        public void OnUpdateState()
        { }
    }
}