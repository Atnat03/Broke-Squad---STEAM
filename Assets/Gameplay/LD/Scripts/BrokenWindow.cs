using Bus;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Gameplay.LD.Scripts
{
    [RequireComponent(typeof(NetworkObject))]
    public class BrokenWindow : NetworkBusListener, IDamageable
    {

        [SerializeField] private float solidity = 100.0f;
        [SerializeField] private Animator brokenWAnimator;
        [SerializeField] private string brokenStateName = "Broken";

        private readonly NetworkVariable<bool> isBroken = new(false);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            isBroken.OnValueChanged += OnBrokenChanged;
        }

        public override void OnNetworkDespawn()
        {
            isBroken.OnValueChanged -= OnBrokenChanged;
            base.OnNetworkDespawn();
        }

        public void ApplyDamage(float damage)
        {
            if (!IsServer || !isBroken.Value) {return;}

            if (damage >= solidity) Break();
        }

        private void Break()
        {
            // Proto 
            NetworkObject.Despawn();

            // Plus tard 
            // isBroken.Value = true;
        }

        private void OnBrokenChanged(bool previous, bool current)
        {
            if (!current) return;

            //brokenWAnimator.Play(brokenStateName, 0);
            //GetComponent<Collider>().enabled = false;
        }
    }
}
