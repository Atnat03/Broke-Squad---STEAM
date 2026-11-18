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
        public float AnimationDuration;
        public Sprite Sprite;
    }
    
    public class PlayerEmoteView : MonoBusListener
    {
        [SerializeField] private PlayerSetUpBridge _bridge;

        [Header("UI")] 
        [SerializeField] private GameObject _uiWheelEmote;
        
        [Header("UI Elements")]
        [SerializeField] private EmoteData[] _emotesDataList;
        [SerializeField] private EmoteElement _emoteElementPrefab;
        [SerializeField] private Transform _elementTransform;
        
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
            int maxCount = _emotesDataList.Length;
            
            for (int i = 0; i < maxCount; i++)
            {
                EmoteElement e = Instantiate(_emoteElementPrefab, _elementTransform);
                e.SetUpElement(_emotesDataList[i], _uiWheelEmote);
            }
            
            _uiWheelEmote.SetActive(false);
        }
    }
}