using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class ActivateSomethingInHandModule : ActionModule, IFirstAction, IServerAction, IAnchorAware
    {
        public override string Emoji => "🥳";
        
        [SerializeField] private string _keyActivationEvent = "ACTIVE_EVENT";
        private bool _active;
        
        private ulong _hostId;
        
        public ICondition Condition => ConditionParent;
        public void StartFirstAction()
        {
            Context.SendToServer(this, new OnModuleDoAction_EVENT());
        }

        public void StopFirstAction() { }

        public void OnNotInHand() { }

        public void OnThrow() { }
        
        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            _active = !_active;
            ABPrint.Print("Activate something in hand : " + _active, ABColor.Pink);
            SendData(_active);
        }

        public void SetAnchor(Transform anchor, ulong hostId)
        {
            _hostId = hostId;
            if (_active) 
                SendData(true);
        }
                
        private void SendData(bool state) => Context.SendEventToEveryone(new OnModuleDoAction_EVENT
        {
            KeyEvent = _keyActivationEvent,
            ValueB = state,
            HostId = _hostId
        });
    }
}