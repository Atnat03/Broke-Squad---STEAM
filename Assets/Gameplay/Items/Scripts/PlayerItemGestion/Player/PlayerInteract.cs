    using System;
    using Bus;
    using Gameplay.Controller;
    using MyPrint;
    using UnityEngine;

    namespace Gameplay.Items
    {
        public class PlayerInteract : NetworkBusListener
        {
            [Header("References")]
            [SerializeField] private PlayerCamera _playerCamera;
            [SerializeField] private PlayerInput _playerInput;
            [SerializeField] private PlayerController _playerController;
            [SerializeField] private PlayerInventory _playerInventory;
            
            [Header("World Settings")]
            [SerializeField] private Camera _camera;
            [SerializeField] private float _maxDistanceToInteract = 2f;
            [SerializeField] private float _maxDistanceToKeepHolding = 2.5f;
            [SerializeField] private LayerMask _interactableMask;
            [SerializeField] private LayerMask _obstacleMask;
            private IInteractable _currentHoverInteractable = null;
            private IHoldInteractable _heldInteractable;
            private Component _heldComponent;

            public Action<bool, Tooltip> OnTooltipChange;
            
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
            private void Update()
            {
                if (!IsOwner || _camera == null) return;
                if (_heldInteractable != null)
                {
                    if (IsHeldTooFar())
                        ReleaseHold();
                    return;
                }
                
                
                
                
                IInteractable target = (_playerCamera != null && _playerCamera.HasOverride) ? null : GetAimingInteractable();

                if (target == _currentHoverInteractable)
                    return;

                if (_currentHoverInteractable != null)
                {
                    _currentHoverInteractable.SetOutline(new StartOutliningData
                    {
                        State = false,
                        Inventory = null
                    });

                    OnTooltipChange?.Invoke(false, new Tooltip
                    {
                        Message = string.Empty,
                        Icon = null
                    });
                }

                _currentHoverInteractable = target;

                if (_currentHoverInteractable == null)
                    return;

                _currentHoverInteractable.SetOutline(new StartOutliningData
                {
                    State = true,
                    Inventory = _playerInventory
                });

                ItemModule[] modules = null;

                ItemInstance currentItem = _playerInventory.GetCurrentItemInHand();

                if (currentItem != null)
                    modules = currentItem.GetAllModules();

                Tooltip data = _currentHoverInteractable.GetTooltip(modules);

                OnTooltipChange?.Invoke(!string.IsNullOrEmpty(data.Message), data);
            }

            private bool IsHeldTooFar()
            {
                if (_heldComponent == null)
                    return true;

                Vector3 origin = _camera.transform.position;
                Vector3 closest;
                
                if (_heldComponent.TryGetComponent(out Collider col))
                {
                    closest = col.ClosestPoint(origin);
                }
                else
                {
                    closest = _heldComponent.transform.position;
                }
                return (closest - origin).sqrMagnitude > _maxDistanceToKeepHolding * _maxDistanceToKeepHolding;
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
                if (target == null)
                    return;
                
                if (target is IHoldInteractable hold)
                {
                    _heldInteractable = hold;
                    _heldComponent = hold as Component;
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
                _heldComponent = null;
            }
        }
    }