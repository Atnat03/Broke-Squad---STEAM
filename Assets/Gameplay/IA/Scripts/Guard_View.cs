using System.Collections;
using Bus;
using MyPrint;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.IA
{
    public class Guard_View : MonoBusListener
    {
        [SerializeField] private GuardBehavior _guard;
        [SerializeField] private GuardLineOfSight _guardLineOfSight;

        [Header("UI")] 
        [Header("Detection")] 
        [SerializeField] private GameObject _detectionUI;
        [SerializeField] private Image _detectionValueImage;
        [Header("Investigation")] 
        [SerializeField] private GameObject _investUI;
        [SerializeField] private Image _investValueImage;
        [Header("Pursuit")] 
        [SerializeField] private GameObject _pursuitUI;
        [Header("LockDown")] 
        [SerializeField] private GameObject _lockDownUI;
        [SerializeField] private Image _lockDownValueImage;
        
        LockDownManager _lockDownManager;
        
        void OnEnable()
        {
            if(_lockDownManager == null)
                _lockDownManager = LockDownManager.instance;

            _lockDownManager.OnStartLockDown += StartLockDown;
            _lockDownManager.OnEndLockDown += StopLockDown;

            _guardLineOfSight.OnDetectionProgressChanged += DetectionUpdate;
            _guardLineOfSight.OnSuspiciousProgressChanged += SuspectUpdate;

            _guardLineOfSight.OnNotifyTargetSeen += UpdateTargetUI;
            //_guardLineOfSight.OnNotifySuspectTargetSeen += UpdateSuspectTargetUI;
        }

        private void StartLockDown()
        {
            _lockDownUI.SetActive(true);
            StartCoroutine(UpdateLockDown());
        }
        
        private void StopLockDown()
        {
            _lockDownUI.SetActive(false);
        }

        IEnumerator UpdateLockDown()
        {
            while (_lockDownManager.IsLockDown)
            {
                yield return null;
                _lockDownValueImage.fillAmount = _lockDownManager.RatioDuration;
            }
        }
        
        private void DetectionUpdate(float ratio)
        {
            _detectionUI.SetActive(ratio is > 0.05f and < 0.99f);
            _detectionValueImage.fillAmount = ratio;
        }
        
        private void SuspectUpdate(float ratio)
        {
            _investUI.SetActive(ratio > 0.05f);
            _investValueImage.fillAmount = ratio;
        }
        
        private void UpdateTargetUI(bool state)
        {
            if(state)
                _investUI.SetActive(false);
            
            _pursuitUI.SetActive(state);
        }
    }
}