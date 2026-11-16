using System.Collections;
using Bus;
using MyPrint;
using Unity.Collections;
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
        
        void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(ApplyFeedback);
        }

        private void ApplyFeedback(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent.Value != _keyEvent) return;
            if(!_canInstantiate) return;
            
            GameObject vfxFeedback = Instantiate(_vfxFeedback, transform.position + _positionOffset, transform.rotation);
            vfxFeedback.transform.parent = transform;
            vfxFeedback.gameObject.layer = gameObject.layer;
            Destroy(vfxFeedback.gameObject, 2f);

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