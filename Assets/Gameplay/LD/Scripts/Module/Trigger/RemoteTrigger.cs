using Gameplay.Items;
using MyPrint;
using UnityEngine;

namespace Gameplay.LD
{
    public class RemoteTrigger : LDTriggerModule
    {
        [SerializeField] private int _idRemote = 0;
            
        protected override void OnInitialize()
        {
            if (!Context.IsServer) return;
            
            ListenToEvent<OnRemoteAction_EVENT>(TryTrigger);
        }

        private void TryTrigger(OnRemoteAction_EVENT data)
        {
            if (data.RemoteId != _idRemote) return;
            
            Raise(new LDActivation(null, transform.position));
        }
    }
}