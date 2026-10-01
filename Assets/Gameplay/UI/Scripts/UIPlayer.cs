using Bus;
using Gameplay.Controller;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.PlayerData
{
    public class UIPlayer : NetworkBusListener
    {
        [Header("Health")]
        [SerializeField] private Slider pvSlider;
        [SerializeField] private TextMeshProUGUI playerHpText;

        [Header("Stamina Bar")]
        [SerializeField] private Slider playerStaminaSlider;
        [SerializeField] private CanvasGroup staminaGroup;
        [SerializeField] private float staminaSmoothSpeed = 12f;
        [SerializeField] private float staminaHideDelay = 1.5f;  
        [SerializeField] private float staminaFadeSpeed = 4f;   
        [Header("References")]
        [SerializeField] private PlayerData playerData;
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

            if (playerData == null)
                playerData = GetComponent<PlayerData>();

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerData != null)
            {
                UpdateUI(new PlayerDataEvent
                {
                    playerHp = playerData.CurrentHp,
                    maxHp = playerData.MaxHp
                });
            }

            if (playerStaminaSlider != null && playerController != null)
            {
                _maxStamina = playerController.MaxStamina;
                _targetStamina = _displayedStamina = _maxStamina;
                playerStaminaSlider.maxValue = _maxStamina;
                playerStaminaSlider.value = _maxStamina;
                _hideTimer = 0f;

                // Start hidden, since stamina is full
                if (staminaGroup != null) staminaGroup.alpha = 0f;
            }
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