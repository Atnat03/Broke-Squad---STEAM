using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay.UI
{
    public class HoverInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private CanvasGroup _hoverInformation;
        [SerializeField] private float _fadeSpeed = 4;
        
        private void Start()
        {
            _hoverInformation.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartCoroutine(FadeIn());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            _hoverInformation.gameObject.SetActive(true);
            
            _hoverInformation.alpha = 0;

            while (_hoverInformation.alpha < 1)
            {
                _hoverInformation.alpha += Time.deltaTime * _fadeSpeed;
                
                yield return null;
            }

            _hoverInformation.alpha = 1;
        }
        
        private IEnumerator FadeOut()
        {
            _hoverInformation.alpha = 1;

            while (_hoverInformation.alpha > 0)
            {
                _hoverInformation.alpha -= Time.deltaTime * _fadeSpeed;
                
                yield return null;
            }

            _hoverInformation.alpha = 0;
            
            _hoverInformation.gameObject.SetActive(false);
        }
    }
}