using Bus;
using Gameplay.Controller;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInteract : NetworkBusListener
    {
        [Header("References")]
        [SerializeField] private PlayerCamera _playerCamera;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private PlayerController _playerController;
        
        [Header("World Settings")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _maxDistanceToInteract = 2f;
        [SerializeField] private LayerMask _interactableMask;
        [SerializeField] private LayerMask _obstacleMask;
        private IInteractable _currentHoverInteractable = null;
        private IHoldInteractable _heldInteractable;
        #region Network Start
        
        public override void OnNetworkSpawn()
        {
            if(IsOwner)
            {
                _playerInput.OnInteractInput += TryToInteract;
                _playerInput.OnInteractInputCanceled += ReleaseHold;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if(IsOwner)
            {
                _playerInput.OnInteractInput -= TryToInteract;
                _playerInput.OnInteractInputCanceled -= ReleaseHold;
                ReleaseHold();
            }
        }
        
        #endregion
        
        // Activate / Desactivate the outline of the IInteractable that we're aiming to
        void Update()
        {
            if (!IsOwner || _camera == null) return;

            IInteractable target = (_playerCamera != null && _playerCamera.HasOverride) ? null : GetAimingInteractable();
            
            if (_heldInteractable != null && !ReferenceEquals(target, _heldInteractable))
                ReleaseHold();
            
            if (target == _currentHoverInteractable) return;
            
            if (target == null && _currentHoverInteractable != null)
                _currentHoverInteractable.SetOutline(false);

            _currentHoverInteractable?.SetOutline(false);
            
            _currentHoverInteractable = target;

            if (_currentHoverInteractable != null)
                _currentHoverInteractable.SetOutline(true);
        }
        
        private IInteractable GetAimingInteractable()
        {
            int rayMask = _interactableMask | _obstacleMask;

            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward,
                    out RaycastHit hit, _maxDistanceToInteract, rayMask, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.TryGetComponent(out IInteractable interact))
                {
                    return interact;
                }
            }

            return null;
        }
        
        // When press E => check if they're an IInteractable in front of us then Interact with it is there is
        private void TryToInteract()
        {
            if (_playerCamera != null && _playerCamera.HasOverride)
            {
                _playerCamera.ReleaseOverride();
                return;
            }
            
            IInteractable target = _currentHoverInteractable ?? GetAimingInteractable();
            if (target == null) return;

            if (target is IHoldInteractable hold)
            {
                _heldInteractable = hold;
                hold.HoldStart(OwnerClientId);
            }
            else
            {
                target.Interact(OwnerClientId);
            }
        }

        private void ReleaseHold()
        {
            if (_heldInteractable == null) return;
            _heldInteractable.HoldEnd(OwnerClientId);
            _heldInteractable = null;
        }
    }
}