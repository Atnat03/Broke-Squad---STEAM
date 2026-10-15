using Bus;
using UnityEngine;

namespace Gameplay.Controller
{
    public struct KeyholeWeightChanged_EVENT
    {
        public float Weight;
    }

    public class KeyholeOverlayView : NetworkBusListener
    {
        [SerializeField] private CanvasGroup blackFade;
        [SerializeField] private CanvasGroup keyholeMask;

        [SerializeField] private float fadeOutTime = 0.4f;

        private float _lastW;
        private bool _fading;
        private float _fadeT;

        private void Awake()
        {
            ListenToEvent<KeyholeWeightChanged_EVENT>(OnWeight);
            blackFade.alpha = 0f;
            keyholeMask.alpha = 0f;
        }

        private void OnWeight(KeyholeWeightChanged_EVENT e)
        {
            float w = e.Weight;

            if (w >= 1f && _lastW < 1f)
            {
                StartFade(maskAlpha: 1f);
            }
            else if (w <= 0f && _lastW > 0f)
            {
                StartFade(maskAlpha: 0f);
            }
            else if (w > 0f && w < 1f)
            {
                _fading = false;
                blackFade.alpha = Mathf.SmoothStep(0f, 1f, w);
                keyholeMask.alpha = 0f;
            }

            _lastW = w;
        }

        private void StartFade(float maskAlpha)
        {
            _fading = true;
            _fadeT = 0f;
            blackFade.alpha = 1f;
            keyholeMask.alpha = maskAlpha;
        }

        private void Update()
        {
            if (!_fading) return;

            _fadeT += Time.deltaTime / Mathf.Max(0.01f, fadeOutTime);
            blackFade.alpha = 1f - Mathf.SmoothStep(0f, 1f, _fadeT);

            if (_fadeT >= 1f)
            {
                _fading = false;
                blackFade.alpha = 0f;
            }
        }
    }
}