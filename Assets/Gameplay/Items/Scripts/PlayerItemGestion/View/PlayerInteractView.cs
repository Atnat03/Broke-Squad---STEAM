using Bus;
using MyPrint;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class PlayerInteractView : MonoBusListener
    {
        [SerializeField] private PlayerSetUpBridge _bridge;

        [Header("UI")]
        [SerializeField] private GameObject _tooltipParent;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        [SerializeField] private Image _tooltipIcon;
        
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

        private void UpdateTooltip(bool isVisible, Tooltip data)
        {
            _tooltipParent.SetActive(isVisible);
            _tooltipText.text = data.Message;
            
            _tooltipIcon.gameObject.SetActive(data.Icon != null);
            _tooltipIcon.sprite = data.Icon;
        }
    }
}