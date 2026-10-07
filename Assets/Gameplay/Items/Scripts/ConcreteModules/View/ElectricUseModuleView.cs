using System;
using System.Collections;
using Bus;
using MyPrint;
using TMPro;
using UnityEngine;

namespace Gameplay.Items
{
    public class ElectricUseModuleView : MonoBusListener
    {
        [SerializeField] private string _eventKey = "ELECTRIC_USE_MODULE";
    
        [Header("References")]
        [SerializeField] private GameObject _uiUse;
        [SerializeField] private TextMeshProUGUI _useText;
        
        [Header("UI Settings")]
        [SerializeField] private float _speedFill = 2f;

        private float _targetFill;
        private float _visualFill = 100f;
        
        private void Awake()
        {
            _uiUse.gameObject.SetActive(false);
            
            _targetFill = _visualFill;
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
            
            // Pour snap direct ou non
            if(data.ValueI == 0)
                _targetFill = data.ValueF;
            else
                _targetFill = _visualFill = data.ValueF;
        }

        private void Update()
        {
            _visualFill = Mathf.MoveTowards(_visualFill, _targetFill, _speedFill * Time.deltaTime);

            _useText.text = Mathf.RoundToInt(_visualFill) + "%";
        }
    }
}