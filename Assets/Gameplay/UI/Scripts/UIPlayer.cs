using Bus;
using Gameplay.Controller;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Gameplay.PlayerData
{
    public class UIPlayer : NetworkBusListener
    {
        [Header("Health")]
        [SerializeField] private Slider pvSlider;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private Color pvColor;
        [SerializeField] private Color invincibleColor;
        [SerializeField] private Image sliderColor;
        

        [Header("Stamina Bar")]
        [SerializeField] private Slider playerStaminaSlider;
        [SerializeField] private CanvasGroup staminaGroup;
        [SerializeField] private float staminaSmoothSpeed = 12f;
        [SerializeField] private float staminaHideDelay = 1.5f;  
        [SerializeField] private float staminaFadeSpeed = 4f;   
        
        [FormerlySerializedAs("playerData")]
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerController playerController;

        private float _targetStamina;
        private float _displayedStamina;
        private float _maxStamina = 1f;
        private float _hideTimer;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                pvSlider.gameObject.SetActive(false);
                if (playerStaminaSlider != null)
                    playerStaminaSlider.gameObject.SetActive(false);
                return;
            }

            pvSlider.gameObject.SetActive(true);

            ListenToEvent<PlayerDataEvent>(UpdateUI);
            ListenToEvent<StaminaChangedEvent>(UpdateUI);

            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
            if (playerController == null) playerController = GetComponent<PlayerController>();

            if (playerHealth != null)
            {
                SetHp(playerHealth.CurrentHp);
                SetInvincible(playerHealth.IsInvincible); 
                playerHealth.OnChangeHp += OnHpChanged;
                playerHealth.OnChangedInvincibility += SetInvincible;
            }

            if (playerStaminaSlider != null && playerController != null)
            {
                _maxStamina = playerController.MaxStamina;
                _targetStamina = _displayedStamina = _maxStamina;
                playerStaminaSlider.maxValue = _maxStamina;
                playerStaminaSlider.value = _maxStamina;
                _hideTimer = 0f;
                
                if (staminaGroup != null) staminaGroup.alpha = 0f;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if (playerHealth != null)
            {
                playerHealth.OnChangeHp -= OnHpChanged;
                playerHealth.OnChangedInvincibility -= SetInvincible;
            }
            base.OnNetworkDespawn();
        }

        private void OnHpChanged(int previous, int current) => SetHp(current);

        private void SetHp(int hp)
        {
            if (pvSlider == null || playerHpText == null) return;
            pvSlider.maxValue = playerHealth.MaxHp;
            pvSlider.value = hp;
            playerHpText.text = hp.ToString();
        }

        private void SetInvincible(bool invincible)
        {
            if (sliderColor != null) sliderColor.color = invincible ? invincibleColor : pvColor;
        }

        private void Update()
        {
            if (!IsOwner || playerStaminaSlider == null) return;
            
            float t = 1f - Mathf.Exp(-staminaSmoothSpeed * Time.deltaTime);
            _displayedStamina = Mathf.Lerp(_displayedStamina, _targetStamina, t);
            if (Mathf.Abs(_displayedStamina - _targetStamina) < 0.01f)
                _displayedStamina = _targetStamina;

            playerStaminaSlider.value = _displayedStamina;
            
            if (staminaGroup == null) return;

            bool full = _targetStamina >= _maxStamina && _displayedStamina >= _maxStamina - 0.01f;
            _hideTimer = full ? _hideTimer - Time.deltaTime : staminaHideDelay;

            float targetAlpha = _hideTimer > 0f ? 1f : 0f;
            staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, targetAlpha, staminaFadeSpeed * Time.deltaTime);
        }

        private void UpdateUI(PlayerDataEvent e)
        {
            if (pvSlider == null || playerHpText == null) return;

            pvSlider.maxValue = e.maxHp;
            pvSlider.value = e.playerHp;
            playerHpText.text = e.playerHp.ToString();
        }

        private void UpdateUI(StaminaChangedEvent e)
        {
            _maxStamina = e.maxStamina;
            _targetStamina = e.stamina;

            if (playerStaminaSlider != null)
                playerStaminaSlider.maxValue = e.maxStamina;
        }
        
    }
}