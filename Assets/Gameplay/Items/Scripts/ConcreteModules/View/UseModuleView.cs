using System;
using Bus;
using MyPrint;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items
{
    public class UseModuleView : MonoBusListener
    {
        [SerializeField] private string _eventKey = "USE_MODULE";
    
        [Header("References")]
        [SerializeField] private GameObject _uiUse;
        [SerializeField] private TextMeshProUGUI _useText;

        private void Awake()
        {
            _uiUse.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(UpdateBar);
        }
    
        private void UpdateBar(OnModuleDoAction_EVENT data)
        {
            if(_eventKey != data.KeyEvent)
                return;
            
            _uiUse.gameObject.SetActive(data.ValueB);
            _useText.text = data.ValueS.Value;
        }
    }
}