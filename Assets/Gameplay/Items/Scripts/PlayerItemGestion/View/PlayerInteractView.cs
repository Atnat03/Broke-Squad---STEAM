using Bus;
using TMPro;
using UnityEngine;

namespace Gameplay.Items
{
    public class PlayerInteractView : MonoBusListener
    {
        [SerializeField] private PlayerInteract _playerInteract;

        [Header("UI")]
        [SerializeField] private GameObject _tooltipParent;
        [SerializeField] private TextMeshProUGUI _tooltipText;
        
        void OnEnable()
        {
            _playerInteract.OnTooltipChange += UpdateTooltip;
            
            _tooltipParent.SetActive(false);
        }
        
        void OnDisable()
        {
            _playerInteract.OnTooltipChange -= UpdateTooltip;
        }

        private void UpdateTooltip(bool isVisible, string txt)
        {
            _tooltipParent.SetActive(isVisible);
            _tooltipText.text = txt;
        }
    }
}