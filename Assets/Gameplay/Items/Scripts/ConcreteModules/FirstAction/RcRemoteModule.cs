using System;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class RcRemoteModule : ActionModule, IFirstAction, IServerAction
    {
        public override string Emoji => "🕹️";
        
        private FixedString32Bytes _keyEvent = "RC_CAR_EVENT";
        
        [SerializeField] private int _carID = 0;
        private bool _isActivated = false;
        
        [Header("Animation")]
        [SerializeField] private string _animationName = "Active";
        
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            _isActivated = !_isActivated;

            Animator animator = Context.Core.GetComponentInChildren<Animator>();
            animator?.SetBool(_animationName, _isActivated);
            
            if(_isActivated)Context.Inventory.Controller.PushLock();
            else Context.Inventory.Controller.PopLock();
            
            SendEvent(_isActivated);
        }

        public void StopFirstAction()
        {
        }

        public void OnNotInHand()
        {
            SendEvent(false);
            Context.Inventory.Controller.PopLock();
        }

        public void OnThrow()
        {
            SendEvent(false);
            Context.Inventory.Controller.PopLock();
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