using Bus;
using TMPro;
using UnityEngine;

namespace Gameplay.Items.PickingUp
{
    public class ElectricHoverInfo : MonoBusListener, IHoverItemInfo
    {
        [Header("References")]
        [SerializeField] private ItemPickup _pickup;
        [SerializeField] private GameObject _hoverInfo;
        [SerializeField] private TextMeshProUGUI _percentageInfo;

        private ElectricUseModule _electricUseModule;

        public void OnHoverUpdate(bool state)
        {
            if (_hoverInfo == null) return;

            _hoverInfo.SetActive(state);

            if (!state)
            {
                _electricUseModule = null;
                return;
            }

            ItemInstance instance = _pickup.Instance;

            _electricUseModule = instance?.GetModule(typeof(ElectricUseModule)) as ElectricUseModule;

            UpdatePercentage();
        }

        private void LateUpdate()
        {
            if (_hoverInfo == null || !_hoverInfo.activeSelf)
                return;

            UpdatePercentage();
        }

        private void UpdatePercentage()
        {
            if (_electricUseModule == null || _percentageInfo == null)
                return;

            _percentageInfo.text = _electricUseModule.GetState() + " %";
        }
    }
}