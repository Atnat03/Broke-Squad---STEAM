using Bus;
using Gameplay.PlayerData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class DownedUI : MonoBusListener
    {
        [Header("References")]
        [SerializeField] private GameObject downedUI;
        [SerializeField] private PlayerSetUpBridge bridge;

        [Header("Revive")]
        [SerializeField] private Image reviveImage;
        [SerializeField] private TextMeshProUGUI reviveText;
        [SerializeField] private float fillDecaySpeed = 0.5f; 

        private bool _reviving;

        public void Awake()
        {
            ListenToEvent<DownedChanged_EVENT>(ToggleDownedUI);
            ListenToEvent<ReviveChanged_EVENT>(OnReviveChanged);
            downedUI.SetActive(false);
        }

        public void ToggleDownedUI(DownedChanged_EVENT e)
        {
            if (e.PlayerId != bridge.PlayerHealth.OwnerClientId) return;

            downedUI.SetActive(e.Downed);
            
            _reviving = false;
            reviveImage.fillAmount = 0f;
            reviveImage.gameObject.SetActive(false);
            reviveText.gameObject.SetActive(false);
        }

        public void OnReviveChanged(ReviveChanged_EVENT e)
        {
            if (e.PlayerId != bridge.PlayerHealth.OwnerClientId) return;

            _reviving = e.Reviving;
            reviveText.gameObject.SetActive(e.Reviving);

            if (e.Reviving)
            {
                reviveImage.gameObject.SetActive(true);
                reviveImage.fillAmount = e.Percent;
            }
        }

        private void Update()
        {
            if (_reviving || !reviveImage.gameObject.activeSelf) return;

            reviveImage.fillAmount = Mathf.MoveTowards(reviveImage.fillAmount, 0f, fillDecaySpeed * Time.unscaledDeltaTime);

            if (reviveImage.fillAmount <= 0f) reviveImage.gameObject.SetActive(false);
        }
    }
}