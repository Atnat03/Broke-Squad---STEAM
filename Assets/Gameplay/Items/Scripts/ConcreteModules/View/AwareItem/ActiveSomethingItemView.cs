using System;
using Bus;
using UnityEngine;

namespace Gameplay.Items
{
    public class ActiveSomethingItemView : AwareItemView
    {
        [SerializeField] private string _keyEvent = "ACTIVE_EVENT";
        
        [SerializeField] private GameObject _activableObject;

        private void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(UpdateObjectState);
        }

        private void UpdateObjectState(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _keyEvent) return;
            if (data.HostId != GetHostId()) return;
            
            _activableObject.SetActive(data.ValueB);
        }
    }
}