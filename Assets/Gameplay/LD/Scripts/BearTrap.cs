using Bus;
using Gameplay.PlayerData;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Gameplay.LD.Scripts
{
    public class BearTrap : NetworkBusListener, IInteractable
    {
        [SerializeField] private int damage;
        [SerializeField] private float rangeDetection;
        [SerializeField] private float triggerTime;
        [SerializeField] private Animator triggerAnimator;
        [SerializeField] private string triggerStateName = "Trigger";

        [Tooltip("Distance max joueur-piège acceptée par le serveur. Doit être supérieure à la portée du raycast du joueur.")]
        [SerializeField] private float interactionRange = 4f;

        private readonly NetworkVariable<bool> isArmed = new(true);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            isArmed.OnValueChanged += OnArmedChanged;
            ApplyVisualTrap(isArmed.Value);
        }

        public override void OnNetworkDespawn()
        {
            isArmed.OnValueChanged -= OnArmedChanged;
            base.OnNetworkDespawn();
        }

        private void OnArmedChanged(bool previous, bool current)
        {
            ApplyVisualTrap(current);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || !isArmed.Value) return;

            PlayerData player = other.GetComponentInParent<PlayerData>();
            if (player == null) return;

            isArmed.Value = false;

            player.TakeDamage(damage);
            SnapClientRpc();
        }

        // Touche E
        public void Interact() => ToggleArmedServerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ToggleArmedServerRpc(RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(senderClientId, out NetworkClient client)) return;

            NetworkObject playerNetworkObject = client.PlayerObject;
            if (playerNetworkObject == null) return;

            float distanceToTrap = Vector3.Distance(playerNetworkObject.transform.position, transform.position);
            if (distanceToTrap > interactionRange) return;

            isArmed.Value = !isArmed.Value;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SnapClientRpc()
        {
            //triggerAnimator.Play(triggerStateName, 0);
            // TODO : Ici pour sfx + vfx
        }

        private void ApplyVisualTrap(bool armed)
        {
            // ouvert si amorcé / fermé sinon, voir avec l'animator
        }
    }
}