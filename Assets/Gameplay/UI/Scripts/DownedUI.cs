using System;
using Bus;
using Gameplay.IA;
using Gameplay.PlayerData;
using UnityEngine;

namespace Gameplay.UI
{
    
    public class DownedUI : MonoBusListener
    {
        [SerializeField] private GameObject downedUI;
        [SerializeField] private PlayerSetUpBridge bridge;
        public void Awake()
        {
            ListenToEvent<DownedChanged_EVENT>(ToggleDownedUI);
            downedUI.SetActive(false);
        }

        public void ToggleDownedUI(DownedChanged_EVENT e)
        {
            if (e.PlayerId != bridge.PlayerHealth.OwnerClientId) return;
            downedUI.SetActive(e.Downed);
        }
    }
}