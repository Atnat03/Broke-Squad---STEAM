using System;
using Bus;
using MyPrint;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Controller.Emote
{
    public struct OnPlayEmote_EVENT
    {
        public string TriggerAnimationName;
        public float AnimationDuration;
    }

    public class EmoteElement : MonoBusListener
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        
        public void SetUpElement(EmoteData emoteData, GameObject parent)
        {
            _iconImage.sprite = emoteData.Sprite;
            
            _button.onClick.AddListener(() =>
            {
                InvokeEvent(new OnPlayEmote_EVENT
                {
                    TriggerAnimationName = emoteData.AnimTriggerName,
                    AnimationDuration =  emoteData.AnimationDuration
                });
                parent.SetActive(false);
            });
        }
    }

}