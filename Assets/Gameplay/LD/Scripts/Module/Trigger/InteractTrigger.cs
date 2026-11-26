using System.Collections;
using System.Collections.Generic;
using Gameplay.Controller;
using Gameplay.Items;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
    [RequireComponent(typeof(Collider))]
    public class InteractTrigger : LDTriggerModule, IInteractable
    {
        [SerializeField] private float _range = 4f;
        public Outline outline { get; private set; }
        
        [SerializeField] TooltipConfig _tooltipConfig;
        
        [Header("Needed Item On it")]
        [SerializeField] private float _timeNeedToTriggerIcon = 2;
        [SerializeField] private TooltipForTrigger _itemIconSpritePrefab;
        [SerializeField] private Transform _itemIconPosition;
        [SerializeField] private SO_Item _itemNeedData;
        Coroutine _seeingRoutine = null;
        PlayerInventory _playerInventoryInstance = null;
        TooltipForTrigger _itemIconInstance;
        
        public Tooltip GetTooltip(ItemModule[] playerModules)
        {
            return _tooltipConfig.GetTooltip(playerModules);
        }

        protected override void OnInitialize()
        {
            gameObject.layer = Context.Element.gameObject.layer;
            GetComponent<Collider>().isTrigger = false;
            outline = Context.Element.GetComponentInChildren<Outline>(true);
            SetOutline(new StartOutliningData
            {
                State = false,
                Inventory = null,
            });
        }
        
        private void OnDisable()
        {
            CancelIcon();   
        }
        
        public void Interact(ulong clientId) => Context.RequestInteract(this);

        public void ServerInteract(NetworkObject player)
        {
            if (!Context.IsServer) return;
            if (Vector3.Distance(player.transform.position, transform.position) > _range) return;;
            Raise(new LDActivation(player, player.transform.position, player.GetComponent<PlayerController>().PlayerName));
        }
        
        #region Tooltip
        
        public void SetOutline(StartOutliningData data)
        {
            if (outline) outline.enabled = data.State;

            if (data.State)
            {
                if (_playerInventoryInstance == null)
                    _playerInventoryInstance = data.Inventory;

                if (_seeingRoutine == null && _itemIconInstance == null)
                    _seeingRoutine = StartCoroutine(SeeingRoutine());
            }
            else
            {
                _playerInventoryInstance = null;
                CancelIcon();
            }
        }

        private void CancelIcon()
        {
            if (_seeingRoutine != null)
            {
                StopCoroutine(_seeingRoutine);
                _seeingRoutine = null; 
            }

            if (_itemIconInstance)
            {
                _itemIconInstance.StartDestroy();
                _itemIconInstance = null;
            }
        }

        private IEnumerator SeeingRoutine()
        {
            yield return new WaitForSeconds(_timeNeedToTriggerIcon);

            CreateItemIcon();

            _seeingRoutine = null;
        }

        private void CreateItemIcon()
        {
            if (!_itemIconSpritePrefab || !_itemIconPosition || !_itemNeedData) return;
            
            TooltipForTrigger tooltip = Instantiate(_itemIconSpritePrefab, _itemIconPosition.position, _itemIconPosition.rotation);

            bool hasItem = false;
            
            if (_playerInventoryInstance)
            {
                hasItem = _playerInventoryInstance.HasThisItemInInventory(_itemNeedData);
            }
            
            tooltip.SetUp(_itemNeedData.Icon, hasItem);
            _itemIconInstance = tooltip;
        }

        #endregion
    }
}