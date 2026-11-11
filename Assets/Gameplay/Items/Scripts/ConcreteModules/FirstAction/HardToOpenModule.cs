using System;
using System.Collections;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class HardToOpenModule : ActionModule, IFirstAction, IServerAction
    {
        [SerializeField] private string _keyEvent = "HARD_TO_OPEN_EVENT_KEY";
        
        [Header("Filling")]
        [SerializeField] private float _fillPerClick = 10f;
        [SerializeField] private float _maxFilling = 100;
        
        [Header("Stamina")]
        [SerializeField] private float _staminaCost = 5;
        
        [Header("Decay")]
        [SerializeField] private float _decayDelay = 0.5f;
        [SerializeField] private float _decayPerSecond = 15f;
        
        [Header("Drop")]
        [SerializeField] private ItemPickup _spawnItem;

        private float _currentFill = 0;
        private Coroutine _decayCoroutine;
        
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            if(Context.Inventory.Controller.Stamina.Current - _staminaCost <= 0)
                return;
            
            Context.Inventory.Controller.Stamina.GainStamina(-_staminaCost);
            
            _currentFill += _fillPerClick;
            UpdateView(true);

            if (_currentFill >= _maxFilling)
            {
                HandleOpenItem();
            }
        }

        public void StopFirstAction()
        {
            if (!CanUse()) return;
            
            if (_decayCoroutine != null)
            {
                Context.Inventory.StopCoroutine(_decayCoroutine);
                _decayCoroutine = null;
            }

            _decayCoroutine = Context.Inventory.StartCoroutine(DecayCoroutine());
        }

        public void OnNotInHand()
        {
            if (_decayCoroutine != null)
            {
                Context.Inventory.StopCoroutine(_decayCoroutine);
                _decayCoroutine = null;
            }
            
            _currentFill = 0;
            
            UpdateView(false);
        }

        public void OnThrow()
        {
            
        }

        private void HandleOpenItem()
        {
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                Position = Context.Camera.transform.position + Context.Camera.transform.forward * 0.5f
            });
            
            UpdateView(false);
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }
        
        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            Context.SpawnGameObject(_spawnItem.gameObject, data.Position, Quaternion.identity);
        }

        IEnumerator DecayCoroutine()
        {
            yield return new WaitForSeconds(_decayDelay);
            
            while (_currentFill >= 0)
            {
                _currentFill -= Time.deltaTime * _decayPerSecond;

                UpdateView(true);
                
                yield return null;
            }
        }

        private void UpdateView(bool isVisible)
        {
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueF = _currentFill / _maxFilling,
                ValueB = isVisible,
            });
        }
    }
}