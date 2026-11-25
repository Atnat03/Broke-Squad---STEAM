using System;
using MyPrint;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class RcRemoteModule : ActionModule, IFirstAction, IServerAction
    {
        public override string Emoji => "🕹️";
        
        private FixedString32Bytes _keyEvent = "RC_CAR_EVENT";
        private FixedString32Bytes _keyEventAnim = "RC_CAR_ANIM_EVENT";
        
        [SerializeField] private int _carID = 0;
        private bool _isActivated = false;
        
        [Header("Animation")]
        [SerializeField] private string _animationName = "Active";
        
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            _isActivated = !_isActivated;
            
            Context.Inventory.InvokeEvent(new OnModuleDoAction_EVENT
            {
                ClientId = Context.Inventory.OwnerClientId,
                KeyEvent = _keyEventAnim,
                ValueB = _isActivated,
                ValueS = _animationName,
                InstanceId = Context.Core.Instance.InstanceId
            });
            
            if(_isActivated)Context.Inventory.Controller.PushLock();
            else Context.Inventory.Controller.PopLock();
            
            SendEvent(_isActivated);
        }

        public void StopFirstAction()
        {
        }

        public void OnNotInHand()
        {
            ABPrint.Print("Not in hand");
            
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