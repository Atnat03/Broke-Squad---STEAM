using System;
using Bus;
using Gameplay.IA;
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
            reviveImage.gameObject.SetActive(false);
            reviveText.gameObject.SetActive(false);
        }

        public void OnReviveChanged(ReviveChanged_EVENT e)
        {
            if (e.PlayerId != bridge.PlayerHealth.OwnerClientId) return;

            reviveImage.gameObject.SetActive(e.Reviving);
            reviveText.gameObject.SetActive(e.Reviving);
            
            if (e.Reviving)
            {
                reviveImage.fillAmount = e.Percent;
            }
            
        }
    }
}