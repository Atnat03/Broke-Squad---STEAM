using System;
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
        [SerializeField] private GuardHealth _guardHealth;
        [SerializeField] private GuardLineOfSight _guardLineOfSight;

        [Header("UI")] 
        
        [Header("HP")] 
        [SerializeField] private Image _hpBarUI;
        
        [Header("Detection")] 
        [SerializeField] private GameObject _detectionUI;
        [SerializeField] private Image _detectionValueImage;
        
        [Header("LockDown Alert")]
        [SerializeField] private GameObject _lockDownUI;
        [SerializeField] private Image _lockDownTimer;
        
        [Header("Investigation")] 
        [SerializeField] private GameObject _investUI;
        [SerializeField] private Image _investValueImage;
        
        [Header("Pursuit")] 
        [SerializeField] private GameObject _pursuitUI;
        
        LockDownManager _lockDownManager;
        
        void Start()
        {
            if(_lockDownManager == null)
                _lockDownManager = LockDownManager.instance;
            
            _guardLineOfSight.OnDetectionProgressChanged += DetectionUpdate;

            _guardLineOfSight.OnNotifyTargetSeen += UpdateTargetUI;
            _guardLineOfSight.OnNotifySuspectTargetSeen += UpdateSuspectTargetUI;

            _guard.OnLockDownAlertChange += UpdateGuardAlert;
        }

        private void UpdateSuspectTargetUI(bool state)
        {
            _investUI.SetActive(state);
        }

        private void DetectionUpdate(float ratio)
        {
            _detectionUI.SetActive(ratio is > 0.05f and < 0.99f);
            _detectionValueImage.fillAmount = ratio;
        }
        
        private void UpdateTargetUI(bool state)
        {
            if(state)
                _investUI.SetActive(false);
            
            _pursuitUI.SetActive(state);
        }

        private void Update()
        {
            _hpBarUI.fillAmount = _guardHealth.HealthRatio;
        }
        
        private void UpdateGuardAlert(float ratio)
        {
            _lockDownUI.SetActive(ratio > 0.05f);
            
            _lockDownTimer.fillAmount = 1-ratio;
        }
    }
}