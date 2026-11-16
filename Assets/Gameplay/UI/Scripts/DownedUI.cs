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
        public void Awake()
        {
            ListenToEvent<DownedChanged_EVENT>(ToggleDownedUI);
            downedUI.SetActive(false);
        }

        public void ToggleDownedUI(DownedChanged_EVENT e)
        {
            downedUI.SetActive(e.Downed);
        }
    }
}