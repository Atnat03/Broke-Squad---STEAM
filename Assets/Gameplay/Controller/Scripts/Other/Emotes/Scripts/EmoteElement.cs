using System;
using Bus;
using MyPrint;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay.Controller.Emote
{
    public struct OnSetAnimationWillPlay_EVENT
    {
        public int EmoteId;
        public float EmoteDuration;
    }
    
    public class EmoteElement : MonoBusListener, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private PlayerEmoteView _emoteView;
        [SerializeField] private int _emoteId;
        [SerializeField] private float _animationDuration;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(_emoteId > 0)
                InvokeEvent(new  OnSetAnimationWillPlay_EVENT { EmoteId = _emoteId,  EmoteDuration = _animationDuration});
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            InvokeEvent(new  OnSetAnimationWillPlay_EVENT { EmoteId = 0, EmoteDuration = 0 });
        }
    }

}