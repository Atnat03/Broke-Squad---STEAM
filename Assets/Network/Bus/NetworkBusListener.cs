using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Bus
{
    public abstract class NetworkBusListener : NetworkBehaviour
    {
        protected List<Action> _unsubscribeActions = new List<Action>();

        protected void ListenToEvent<T>(Action<T> listeningAction) where T : struct
        {
            _unsubscribeActions.Add(EventBus.Subscribe(listeningAction));
        }

        protected void InvokeEvent<T>(T newEvent) where T : struct
        {
            EventBus.InvokeEvent(newEvent);
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeAll();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            UnsubscribeAll();
        }

        private void UnsubscribeAll()
        {
            foreach (Action unsubscribeAction in _unsubscribeActions)
                unsubscribeAction?.Invoke();
        }
    }
}
