using System;
using Bus;
using MyPrint;
using TMPro;
using UnityEngine;

namespace Gameplay.Items.PickingUp
{
    public class ElectricHoverInfo : MonoBusListener, IHoverItemInfo
    {
        [SerializeField] private ItemPickup _pickup;
        [SerializeField] private GameObject _hoverInfo;
        [SerializeField] private TextMeshProUGUI _percentageInfo;

        private ItemInstance _instance;
        private ElectricUseModule _electricUseModule;
        
        private void Start()
        {
            _instance = _pickup.Instance;
        }

        public void OnHoverUpdate(bool state)
        {
            if (!_hoverInfo) return; 
            
            _hoverInfo.SetActive(state);
            
            if (_instance != null)
            {
                _electricUseModule = (ElectricUseModule)_instance.GetModule(typeof(ElectricUseModule));
            }
        }

        void LateUpdate()
        {
            if (_instance != null && _hoverInfo.activeSelf)
            {
                if (_electricUseModule != null)
                {
                    _percentageInfo.text = _electricUseModule.GetState() + " %";
                }
            }
        }
    }
}