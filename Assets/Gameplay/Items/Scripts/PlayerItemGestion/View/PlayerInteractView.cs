using Bus;
using TMPro;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInteractView : MonoBusListener
    {
        [SerializeField] private PlayerSetUpBridge _bridge;

        [Header("UI")]
        [SerializeField] private GameObject _tooltipParent;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        
        void Start()
        {
            if(_bridge)
                _bridge.PlayerInteract.OnTooltipChange += UpdateTooltip;
            
            _tooltipParent.SetActive(false);
        }
        
        void OnDisable()
        {
            if(_bridge)
                _bridge.PlayerInteract.OnTooltipChange -= UpdateTooltip;
        }

        private void UpdateTooltip(bool isVisible, string txt)
        {
            _tooltipParent.SetActive(isVisible);
            _tooltipText.text = txt;
        }
    }
}