using System;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class RcRemoteModule : ActionModule, IFirstAction, IServerAction
    {
        private FixedString32Bytes _keyEvent = "RC_CAR_EVENT";
        
        [SerializeField] private int _carID = 0;
        private bool _isActivated = false;
        
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            _isActivated = !_isActivated;
            SendEvent(_isActivated);
        }

        public void StopFirstAction()
        {
        }

        public void OnNotInHand()
        {
            SendEvent(false);
        }

        public void OnThrow()
        {
            SendEvent(false);
        }

        private void SendEvent(bool state)
        {
            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEvent,
                ValueB = state,
                ValueI = _carID
            });
        }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            Context.Inventory.InvokeEvent(data);
        }
    }
}