using Bus;
using Gameplay.PlayerData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Scripts
{
    public class HealthBarView : MonoBusListener
    {
        [SerializeField] private Slider pvSlider;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private Color pvColor = Color.red;
        [SerializeField] private Color invincibleColor = Color.yellow;
        [SerializeField] private Image sliderColor;
        
        private void OnEnable() => ListenToEvent<PlayerHealthChanged_EVENT>(OnPlayerDataChanged);

        void OnPlayerDataChanged(PlayerHealthChanged_EVENT e)
        {
            OnHpChanged(e.CurrentHp, e.MaxHp);
            OnInvincibilityChanged(e.Invincible);
        }
        

        
        private void OnHpChanged(int hp, int maxHp)
        {
            if (pvSlider != null)
            {
                pvSlider.maxValue = maxHp;
                pvSlider.value = hp;
            }
            if (playerHpText != null) playerHpText.text = hp.ToString();
        }

        private void OnInvincibilityChanged(bool invincible)
        {
            if (sliderColor != null) sliderColor.color = invincible ? invincibleColor : pvColor;
        }
    }
}