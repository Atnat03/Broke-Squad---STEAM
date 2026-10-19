using Bus;
using Gameplay.Controller;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInteract : NetworkBusListener
    {
        [SerializeField] private PlayerCamera _playerCamera;
        [Header("References")]
        [SerializeField] private PlayerInput _playerInput;
        
        [Header("World Settings")]
        [SerializeField] private Camera _camera;
        [SerializeField] private float _maxDistanceToInteract = 2f;
        [SerializeField] private LayerMask _interactableMask;
        [SerializeField] private LayerMask _obstacleMask;
        private IInteractable _currentHoverInteractable = null;
        
        #region Network Start
        
        public override void OnNetworkSpawn()
        {
            if(IsOwner)
            {
                _playerInput.OnInteractInput += TryToInteract;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if(IsOwner)
            {
                _playerInput.OnInteractInput -= TryToInteract;
            }
        }
        
        #endregion
        
        // Activate / Desactivate the outline of the IInteractable that we're aiming to
        void Update()
        {
            if (!IsOwner || _camera == null) return;

            IInteractable target = (_playerCamera != null && _playerCamera.HasOverride) ? null : GetAimingInteractable();
            
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
                    out RaycastHit hit, _maxDistanceToInteract, rayMask, QueryTriggerInteraction.Ignore))
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
            
            if (_currentHoverInteractable != null)
            {
                _currentHoverInteractable.Interact(OwnerClientId);
            }
            else
            {
                IInteractable target = GetAimingInteractable();

                if (target != null)
                {
                    target.Interact(OwnerClientId);
                }
            }
        }
    }
}