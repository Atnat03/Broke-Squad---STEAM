using System;
using System.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class HardToOpenModule : ActionModule, IFirstAction
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
            
            if (_decayCoroutine != null)
            {
                Context.Inventory.StopCoroutine(_decayCoroutine);
                _decayCoroutine = null;
            }

            _currentFill += _fillPerClick;

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

            _decayCoroutine = Context.StartCoroutine(DecayCoroutine());
        }

        public void OnNotInHand()
        {
            UpdateView(false);
        }

        private void HandleOpenItem()
        {
            Context.SpawnGameObject(_spawnItem.gameObject, Context.Camera.transform.forward, Quaternion.identity);
            
            ConsumeCondition(new OnModuleDoAction_EVENT());
        }

        IEnumerator DecayCoroutine()
        {
            yield return new WaitForSeconds(_decayDelay);
            
            while (_currentFill <= 0)
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