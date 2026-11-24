using Bus;
using Gameplay.PlayerData;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Scripts
{
    public class DamageHealFeedbackView : MonoBusListener
    {
        [Header("References")]
        [SerializeField] private Image vignette;

        [Header("Colors")]
        [SerializeField] private Color damageColor = new Color(0.8f, 0f, 0f, 1f);
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.3f, 1f);

        [Header("Damage")]
        [SerializeField, Range(0f, 1f)] private float minDamageAlpha = 0.35f; 
        [SerializeField, Range(0f, 1f)] private float maxDamageAlpha = 0.9f; 
        [SerializeField] private float damageFadeDuration = 0.6f;

        [Header("Heal")]
        [SerializeField, Range(0f, 1f)] private float healAlpha = 0.3f;
        [SerializeField] private float healFadeDuration = 0.5f;

        [Header("Low HP (permanent)")]
        [SerializeField, Range(0f, 1f)] private float lowHpThreshold = 0.3f; 
        [SerializeField, Range(0f, 1f)] private float lowHpAlpha = 0.25f;

        private float _previousHp;
        private bool _initialized;

        private float _flashAlpha;    
        private float _flashFadeSpeed; 
        private Color _flashColor;
        private float _lowHpTarget;

        public void Awake()
        {
            ListenToEvent<PlayerHealthChanged_EVENT>(OnHealthChanged);
            SetVignette(damageColor, 0f);
        }

        private void OnHealthChanged(PlayerHealthChanged_EVENT e)
        {
            if (!_initialized)
            {
                _initialized = true;
                _previousHp = e.CurrentHp;
                UpdateLowHp(e);
                return;
            }

            float delta = e.CurrentHp - _previousHp;
            _previousHp = e.CurrentHp;

            if (delta < 0f)
            {
                float ratio = Mathf.Clamp01(-delta / e.MaxHp);
                float alpha = Mathf.Lerp(minDamageAlpha, maxDamageAlpha, ratio * 4f); 
                StartFlash(damageColor, alpha, damageFadeDuration);
            }
            else if (delta > 0f)
            {
                StartFlash(healColor, healAlpha, healFadeDuration);
            }

            UpdateLowHp(e);
        }

        private void UpdateLowHp(PlayerHealthChanged_EVENT e)
        {
            bool isLow = e.CurrentHp > 0f && e.CurrentHp / e.MaxHp <= lowHpThreshold;
            _lowHpTarget = isLow ? lowHpAlpha : 0f;
        }

        private void StartFlash(Color color, float alpha, float duration)
        {
            _flashColor = color;
            _flashAlpha = alpha;
            _flashFadeSpeed = alpha / Mathf.Max(0.01f, duration);
        }

        private void Update()
        {
            if (vignette == null) return;

            if (_flashAlpha > 0f) _flashAlpha = Mathf.Max(0f, _flashAlpha - _flashFadeSpeed * Time.unscaledDeltaTime);
            
            float low = _lowHpTarget > 0f ? _lowHpTarget * (0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f)) : 0f;

            if (_flashAlpha >= low) SetVignette(_flashColor, _flashAlpha);
            else SetVignette(damageColor, low);
        }

        private void SetVignette(Color color, float alpha)
        {
            color.a = alpha;
            vignette.color = color;
            vignette.enabled = alpha > 0.001f;
        }
    }
}