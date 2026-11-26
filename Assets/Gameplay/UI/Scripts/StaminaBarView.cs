using Bus;
using Gameplay.Controller;
using Gameplay.PlayerData;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class StaminaBarView : MonoBusListener
    {
        [SerializeField] private Image playerStaminaSlider;
        [SerializeField] private CanvasGroup staminaGroup;
        [SerializeField] private float staminaSmoothSpeed = 12f;
        [SerializeField] private float staminaHideDelay = 1.5f; 
        [SerializeField] private float staminaFadeSpeed = 4f;
        
        [SerializeField] private Color normalStaminaColor = Color.white;
        [SerializeField] private Color exhaustedColor = Color.red;
        [SerializeField] private Color exhaustedFlashColor = Color.black;
        [SerializeField] private float exhaustedFlashSpeed = 4f;
        
        private float _target;
        private float _displayed;
        private float _max = 1f;
        private float _hideTimer;
        private bool _initialized;

        public Image _staminaFill;
        private void OnEnable()
        {
            ListenToEvent<StaminaChanged_EVENT>(OnStaminaChanged);
        }
        
        private void OnStaminaChanged(StaminaChanged_EVENT e)
        {
            _max = e.MaxStamina;
            _target = e.Current;
            
            if (!_initialized)
            {
                _initialized = true;
                _displayed = _target;
                if (staminaGroup != null) staminaGroup.alpha = 0f;
            }
        }
        

        private void Update()
        {
            if (!_initialized || playerStaminaSlider == null) return;

            float t = 1f - Mathf.Exp(-staminaSmoothSpeed * Time.deltaTime);
            _displayed = Mathf.Lerp(_displayed, _target, t);
            if (Mathf.Abs(_displayed - _target) < 0.01f) _displayed = _target;

            playerStaminaSlider.fillAmount = _displayed/_max;

            if (staminaGroup == null) return;

            bool full = _target >= _max && _displayed >= _max - 0.01f;
            _hideTimer = full ? _hideTimer - Time.deltaTime : staminaHideDelay;
            
            float targetAlpha = _hideTimer > 0f ? 1f : 0f;
            staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, targetAlpha, staminaFadeSpeed * Time.deltaTime);
            
            bool exhausted = _target <= 0f;

            if (_staminaFill != null)
            {
                if (exhausted)
                {
                    float flash = Mathf.PingPong(Time.time * exhaustedFlashSpeed, 1f);
                    _staminaFill.color = Color.Lerp(exhaustedFlashColor, exhaustedColor, flash);
                }
                else
                {
                    _staminaFill.color = normalStaminaColor;
                }
            }
        }
    }
}
