
using System.Collections;
using Bus;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    public class ModuleFeedbackView : MonoBusListener
    {
        [SerializeField] private string _keyEvent;
        [SerializeField] private GameObject _vfxFeedback;
        [SerializeField] private Vector3 _positionOffset;

        private bool _canInstantiate = true;
        private float _bufferInstantiate = 0.5f;

        private void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(ApplyFeedback);
        }

        private ItemInstance GetItemInstance()
        {
            ItemPickup pickup = transform.root.GetComponentInChildren<ItemPickup>();

            if (pickup != null)
                return pickup.Instance;

            ItemCore core = transform.root.GetComponentInChildren<ItemCore>();

            if (core != null)
                return core.Instance;

            return null;
        }

        private void ApplyFeedback(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent.Value != _keyEvent)
                return;

            ItemInstance instance = GetItemInstance();

            if (instance == null)
                return;
            
            if (data.InstanceId != instance.InstanceId)
                return;

            if (!_canInstantiate)
                return;

            GameObject vfxFeedback = Instantiate(_vfxFeedback, transform.position + _positionOffset, transform.rotation);

            vfxFeedback.transform.SetParent(transform);
            vfxFeedback.layer = gameObject.layer;

            Destroy(vfxFeedback, 2f);

            StartCoroutine(BufferingInstantiation());
        }

        private IEnumerator BufferingInstantiation()
        {
            _canInstantiate = false;

            yield return new WaitForSeconds(_bufferInstantiate);

            _canInstantiate = true;
        }
    }
}
