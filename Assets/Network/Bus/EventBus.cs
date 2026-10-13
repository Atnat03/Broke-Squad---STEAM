using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bus
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>>  _handlers = new Dictionary<Type, List<Delegate>>();

        public static Action Subscribe<T>(Action<T> handler) where T : struct
        {
            Type type = typeof(T);
            if (!_handlers.ContainsKey(type))
            {
                _handlers[type] = new List<Delegate>();
            }
            
            _handlers[type].Add(handler);
            return () => Unsubscribe(handler);
        }
        
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (_handlers.ContainsKey(typeof(T)))
            {
                _handlers[typeof(T)].Remove(handler);
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        public static void InvokeEvent<T>(T invokedEvent) where T : struct
        {
            if(!_handlers.TryGetValue(typeof(T), out List<Delegate> subscribers)) return;

            for (int _i = subscribers.Count - 1; _i >= 0; _i--)
            {
                try
                {
                    ((Action<T>)subscribers[_i]).Invoke(invokedEvent);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogException(e);
#endif
                }
            }
        }
    }
}