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
        
        
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerController playerController;

        private float _targetStamina;
        private float _displayedStamina;
        private float _hideTimer;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                pvSlider.gameObject.SetActive(false);
                return;
            }

            pvSlider.gameObject.SetActive(true);

            //ListenToEvent<PlayerDataEvent>(UpdateUI);

            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
            if (playerController == null) playerController = GetComponent<PlayerController>();

            if (playerHealth != null)
            {
                SetHp(playerHealth.CurrentHp, playerHealth.MaxHp);
                SetInvincible(playerHealth.IsInvincible); 
                playerHealth.OnChangeHp += OnHpChanged;
                playerHealth.OnChangedInvincibility += SetInvincible;
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

        private void OnHpChanged(float current, float max) => SetHp(current, max);

        private void SetHp(float hp, float max)
        {
            if (pvSlider == null || playerHpText == null) return;
            pvSlider.maxValue = max;
            pvSlider.value = hp;
            playerHpText.text = ((int)hp).ToString();
        }

        private void SetInvincible(bool invincible)
        {
            if (sliderColor != null) sliderColor.color = invincible ? invincibleColor : pvColor;
        }

        private void UpdateUI(PlayerHealthChanged_EVENT e)
        {
            if (pvSlider == null || playerHpText == null) return;

            pvSlider.maxValue = e.MaxHp;
            pvSlider.value = e.CurrentHp;
            playerHpText.text = ((int)e.CurrentHp).ToString();
        }
        
        
    }
}