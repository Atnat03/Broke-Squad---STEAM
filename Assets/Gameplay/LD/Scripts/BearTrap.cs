using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Gameplay.LD.Scripts
{
    public class BearTrap : NetworkBusListener
    {
        [SerializeField] private int damage;
        [SerializeField] private float rangeDection;
        [SerializeField] private float triggerTime;
        [SerializeField] private Animator triggerAnimator;
        [SerializeField] private string triggerStateName = "Trigger";

        private readonly NetworkVariable<bool> isArmed = new(false);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            isArmed.OnValueChanged += OnArmedChanged;
            ApplyVisualState(isArmed.Value);
        }

        public override void OnNetworkDespawn()
        {
            isArmed.OnValueChanged -= OnArmedChanged;
            base.OnNetworkDespawn();
        }

        private void OnArmedChanged(bool previous, bool current)
        {
            if (!current) return;

            ApplyVisualState(current);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !isArmed.Value) return;

            PlayerData player = other.GetComponent<PlayerData>();
            if (player == null) return;

            isArmed.Value = false;

            player.TakeDamage(damage);

            SnapClientRpc();
        }

        // Call le script pour la touche E 
        public void RequestToggle() => ToggleServerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ToggleServerRpc()
        {
            // Check avec antoine 
            isArmed.Value = !isArmed.Value;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SnapClientRpc()
        {
            //triggerAnimator.Play(triggerStateName, 0);
            // TODO : Ici pour sfx + vfx
        }

        private void ApplyVisualState(bool armed)
        {
            //ouvert si amorcé / fermé sinon  voir avec l'animator
        }
    }
}
