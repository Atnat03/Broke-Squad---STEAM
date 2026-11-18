using System;
using Bus;
using MyPrint;
using TMPro;
using UnityEngine;

namespace Gameplay.Controller.Emote
{
    [Serializable]
    public struct EmoteData
    {
        public string AnimTriggerName;
        public EmoteElement AnimationDuration;
    }
    
    public class PlayerEmoteView : MonoBusListener
    {
        [SerializeField] private PlayerSetUpBridge _bridge;

        [Header("UI")] 
        [SerializeField] private GameObject _uiWheelEmote;
        
        private void OnEnable()
        {
            ListenToEvent<OnEmoteWheelShow_EVENT>(UpdateWheelState);
        }

        private void UpdateWheelState(OnEmoteWheelShow_EVENT data)
        {
            _uiWheelEmote.SetActive(data.State);
        }

        private void Start()
        {
            _uiWheelEmote.SetActive(false);
        }
    }
}