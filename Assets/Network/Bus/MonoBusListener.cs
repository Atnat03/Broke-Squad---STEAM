using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bus
{
    public class MonoBusListener : MonoBehaviour
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

        protected virtual void OnDestroy()
        {
            UnsubscribeAll();
        }

        protected void UnsubscribeAll()
        {
            foreach (Action unsubscribeAction in _unsubscribeActions)
                unsubscribeAction?.Invoke();
        }
    }
}