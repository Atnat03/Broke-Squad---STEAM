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
        [SerializeField] private int _numberOfUse = 3;
        [SerializeField] private ConsequenceOfNoUse _destroyWhenEmpty = ConsequenceOfNoUse.DestroyObject;
        private bool _canUse = true;
        
        private FixedString32Bytes _keyEvent = "USE_MODULE";

        private int _currentUse;

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

        public bool CheckCondition() => _currentUse > 0 && _canUse;

        public void ServerExecute(OnModuleDoAction_EVENT data) => UseItem();

        public void UseItem()
        {
            if (_currentUse <= 0) return;
            
            _currentUse--;
           
            Context.SendEventTo(Context.Inventory.OwnerClientId, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = true,
                ValueS = _currentUse + "/"+ _numberOfUse,
            });


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
                    Context.Inventory.DestroyItemInHand();
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