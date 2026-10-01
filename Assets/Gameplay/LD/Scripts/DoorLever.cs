using Bus;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Gameplay.LD.Scripts
{
    public class DoorLever : NetworkBusListener, IInteractable
    {
        [SerializeField] private int _doorID;
        [SerializeField] private GameObject _door;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _activateStateName = "Activate";
        [SerializeField] private string _deactivateStateName = "Deactivate";

        [SerializeField] private float _interactionRange = 4f;

        private readonly NetworkVariable<bool> _isActivated = new(false);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isActivated.OnValueChanged += OnActivatedChanged;

            PlayLeverAnimation(_isActivated.Value, normalizedTime: 1f);
        }

        public override void OnNetworkDespawn()
        {
            _isActivated.OnValueChanged -= OnActivatedChanged;
            base.OnNetworkDespawn();
        }

        private void OnActivatedChanged(bool previous, bool current)
        {
            PlayLeverAnimation(current, normalizedTime: 0f);
        }

        private void PlayLeverAnimation(bool activated, float normalizedTime)
        {
            string stateName = activated ? _activateStateName : _deactivateStateName;

            if (!_animator.HasState(0, Animator.StringToHash(stateName)))
            {
                Debug.LogWarning($"DoorLever: état '{stateName}' introuvable dans l'Animator", this);
                return;
            }

            _animator.Play(stateName, 0, normalizedTime);
        }

        // Touche E
        public void Interact() => ToggleLeverServerRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ToggleLeverServerRpc(RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(senderClientId, out NetworkClient client)) return;

            NetworkObject playerNetworkObject = client.PlayerObject;
            if (playerNetworkObject == null) return;

            float distanceToLever = Vector3.Distance(playerNetworkObject.transform.position, transform.position);
            if (distanceToLever > _interactionRange) return;

            if (_door == null || !_door.TryGetComponent(out Door door))
            {
                Debug.LogWarning("DoorLever: la porte assignée est vide ou n'a pas de script Door", this);
                return;
            }

            if (!door.TryOpen(_doorID)) return;

            _isActivated.Value = !_isActivated.Value;
        }
    }
}