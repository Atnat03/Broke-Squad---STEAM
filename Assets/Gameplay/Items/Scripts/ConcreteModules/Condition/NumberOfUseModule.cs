using System;
using MyPrint;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    public enum ConsequenceOfNoUse{DestroyObject, DisableAction}
    
    [Serializable]
    public class NumberOfUseModule : ItemModule, ICondition, IServerAction, IReplicatedModule
    {
        public override string Emoji => "🔢";
        private FixedString32Bytes _keyEvent = "USE_MODULE";
        
        [SerializeField] private int _numberOfUse = 3;
        [SerializeField] private ConsequenceOfNoUse _destroyWhenEmpty = ConsequenceOfNoUse.DestroyObject;
        [SerializeField] private string _keyEventCantUse = "CANT_USE_NUMBER";
        
        private bool _canUse = true;

        private int _currentUse;
        
        public bool HasToBeDestroyed => _currentUse == 0;

        protected override void SetModule()
        {
            _currentUse = _numberOfUse;
            _canUse = true;
        }

        protected override void OnComeInHand()
        {
            if (!Context.Inventory.IsServer) return;
            
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = true,
                ValueS = _currentUse + "/" + _numberOfUse,
            });
        }

        public bool CheckCondition()
        {
            bool r = _currentUse > 0 && _canUse;
            
            if (!r)
            {
                Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
                {
                    KeyEvent = _keyEventCantUse,
                });
            }

            return r;
        }

        public void ServerExecute(OnModuleDoAction_EVENT data) => UseItem();

        public void UseItem()
        {
            if (_currentUse <= 0)
                return;

            _currentUse--;

            if (Context.Inventory.IsItemInHand(Context.Core.Instance))
            {
                Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
                {
                    ClientId = Context.Inventory.OwnerClientId,
                    KeyEvent = _keyEvent,
                    ValueB = true,
                    ValueS = _currentUse + "/" + _numberOfUse,
                });
            }

            if (_currentUse <= 0)
            {
                ConsequenceOfOutOfUse();
            }

            Context.NotifyStateChanged(this);
        }

        private void ConsequenceOfOutOfUse()
        {
            switch (_destroyWhenEmpty)
            {
                case ConsequenceOfNoUse.DestroyObject:
                    Context.Inventory.RemoveItemFromInventory(Context.Core.Instance);
                    return;
                case ConsequenceOfNoUse.DisableAction:
                    ABPrint.Print("This action is disable !", ABColor.Red);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public void DisableItemInHand()
        {
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = false,
            });
        }
        
        public int GetState() => _currentUse;
        public void SetState(int value) => _currentUse = Mathf.Clamp(value, 0, _numberOfUse);
    }
}