using Gameplay.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class StaminaBarView : MonoBehaviour
    {
        [SerializeField] private Slider playerStaminaSlider;
        [SerializeField] private CanvasGroup staminaGroup;
        [SerializeField] private float staminaSmoothSpeed = 12f;
        [SerializeField] private float staminaHideDelay = 1.5f;
        [SerializeField] private float staminaFadeSpeed = 4f;

        private PlayerController _controller;
        private float _target;
        private float _displayed;
        private float _max = 1f;
        private float _hideTimer;

        public void Bind(PlayerController controller)
        {
            Unbind();
            _controller = controller;
            //_controller.OnChangedStamina += OnStaminaChanged;

            _max = controller.MaxStamina;
            _target = _displayed = _max;
            _hideTimer = 0f;

            if (playerStaminaSlider != null)
            {
                playerStaminaSlider.maxValue = _max;
                playerStaminaSlider.value = _max;
            }
            if (staminaGroup != null) staminaGroup.alpha = 0f;   
        }

        private void Unbind()
        {
            if (_controller == null) return;
            //_controller.OnChangedStamina -= OnStaminaChanged;
            _controller = null;
        }

        private void OnDestroy() => Unbind();

        private void OnStaminaChanged(float stamina, float max)
        {
            _target = stamina;
            _max = max;
            if (playerStaminaSlider != null) playerStaminaSlider.maxValue = max;
        }

        private void Update()
        {
            if (_controller == null || playerStaminaSlider == null) return;

            float t = 1f - Mathf.Exp(-staminaSmoothSpeed * Time.deltaTime);
            _displayed = Mathf.Lerp(_displayed, _target, t);
            if (Mathf.Abs(_displayed - _target) < 0.01f) _displayed = _target;

            playerStaminaSlider.value = _displayed;

            if (staminaGroup == null) return;

            bool full = _target >= _max && _displayed >= _max - 0.01f;
            _hideTimer = full ? _hideTimer - Time.deltaTime : staminaHideDelay;

            float targetAlpha = _hideTimer > 0f ? 1f : 0f;
            staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, targetAlpha, staminaFadeSpeed * Time.deltaTime);
        }
    }
}
