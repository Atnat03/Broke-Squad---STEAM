using Bus;
using MyPrint;
using UnityEngine;
using UnityEngine.EventSystems;

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
        [SerializeField] private AnimationClip _animationClip;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            ABPrint.Print("OnPointerEnter", ABColor.Green);
            InvokeEvent(new  OnSetAnimationWillPlay_EVENT { EmoteId = _emoteId,  EmoteDuration = _animationClip.length});
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ABPrint.Print("OnPointerExit", ABColor.Red);
            InvokeEvent(new  OnSetAnimationWillPlay_EVENT { EmoteId = -1, EmoteDuration = 0 });

        }
    }

}