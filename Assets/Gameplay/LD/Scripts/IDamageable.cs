using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD.Scripts
{
    public interface IDamageable
    {
        public void ApplyDamage(float damage, NetworkObject source = null);
    }
}