using Bus;
using Gameplay.Controller;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Equipment
{
    public class PlayerEquipment : NetworkBusListener
    {
        [SerializeField] private Transform _packPackParent;
        [SerializeField] private PlayerInput _playerInputs;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _rangeDrop = 2f;

        BackPack _currentBackPack; // résolu sur tous les peers

        readonly NetworkVariable<NetworkObjectReference> _backPackRef = new();
        readonly NetworkVariable<bool> _hasBackPack = new();

        public override void OnNetworkSpawn()
        {
            _hasBackPack.OnValueChanged += OnHasBackPackChanged;

            if (IsOwner)
                _playerInputs.OnTryDropEquipment += TryDropBackPack;

            ListenToEvent<OnTryToPickUpBackPack_EVENT>(TryPickUp);
        }

        public override void OnNetworkDespawn()
        {
            _hasBackPack.OnValueChanged -= OnHasBackPackChanged;

            if (IsOwner)
                _playerInputs.OnTryDropEquipment -= TryDropBackPack;
        }

        private bool ResolveBackPack()
        {
            if (_currentBackPack != null) return true;
            if (!_hasBackPack.Value) return false;

            if (_backPackRef.Value.TryGet(out NetworkObject no) && no.TryGetComponent(out BackPack bp))
            {
                _currentBackPack = bp;
                if (IsOwner) SetBackpackVisibility(false);
                return true;
            }
            return false;
        }

        private void OnHasBackPackChanged(bool previous, bool has)
        {
            if (has || _currentBackPack == null) return;

            if (IsOwner) SetBackpackVisibility(true);
            _currentBackPack = null;
        }

        private void TryPickUp(OnTryToPickUpBackPack_EVENT ev)
        {
            if (ev.ClientId != OwnerClientId) return;
            if (_hasBackPack.Value) return;

            PickupBP(ev.BackPackNOR);
        }

        private void PickupBP(NetworkObjectReference bp)
        {
            if (!IsServer)
            {
                PickupBPServerRpc(bp);
                return;
            }

            if (_hasBackPack.Value) return;
            if (!bp.TryGet(out NetworkObject backpackObject)) return;
            if (!backpackObject.TryGetComponent(out BackPackPhysics physics) || physics.IsCarried) return;

            physics.SetCarried(true);
            _backPackRef.Value = backpackObject;   // <-- la ligne qui manquait
            _hasBackPack.Value = true;
        }

        [Rpc(SendTo.Server)]
        private void PickupBPServerRpc(NetworkObjectReference bp) => PickupBP(bp);

        private void TryDropBackPack()
        {
            Vector3 origin = _camera.transform.position;
            Vector3 fwd = _camera.transform.forward;

            if (Physics.Raycast(origin, fwd, out RaycastHit hit, _rangeDrop, ~0, QueryTriggerInteraction.Ignore))
                TryDrop(hit.point + hit.normal * 0.3f);
            else
                TryDrop(origin + fwd * _rangeDrop);
        }

        private void TryDrop(Vector3 position)
        {
            if (!IsServer)
            {
                TryDropBackPackServerRpc(position);
                return;
            }

            if (!_hasBackPack.Value) return;

            if (ResolveBackPack() && _currentBackPack.TryGetComponent(out BackPackPhysics physics))
                physics.DropAt(position);

            _hasBackPack.Value = false; // déclenche OnHasBackPackChanged (visibilité + null)
        }

        [Rpc(SendTo.Server)]
        private void TryDropBackPackServerRpc(Vector3 position) => TryDrop(position);

        void LateUpdate()
        {
            if (!_hasBackPack.Value) return;
            if (!ResolveBackPack()) return;

            _currentBackPack.transform.SetPositionAndRotation(
                _packPackParent.position, _packPackParent.rotation);
        }

        private void SetBackpackVisibility(bool visible)
        {
            if (_currentBackPack == null) return;

            foreach (Renderer r in _currentBackPack.GetComponentsInChildren<Renderer>(true))
                r.enabled = visible;
        }
    }
}