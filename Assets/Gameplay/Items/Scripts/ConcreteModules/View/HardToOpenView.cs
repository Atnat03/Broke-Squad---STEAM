using System;
using Bus;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class HardToOpenView : NetworkBusListener
    {
        [SerializeField] private string _keyEvent = "HARD_TO_OPEN_EVENT_KEY";
        
        [Header("UI")]
        [SerializeField] private Image _fillImage;

        private void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(UpdateFill);
        }

        private void UpdateFill(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _keyEvent) return;
            
            _fillImage.transform.parent.gameObject.SetActive(data.ValueB);
            
            _fillImage.fillAmount = data.ValueF;
        }
    }
}