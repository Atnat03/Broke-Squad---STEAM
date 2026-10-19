using System;
using Bus;
using Gameplay.IA;
using MyPrint;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class LockDownUI : MonoBusListener
    {
        [Header("UI")]
        [SerializeField] private GameObject _lockDownUI;
        [SerializeField] private Image _lockDownCooldownImage;
        
        private LockDownManager _manager;

        private void Start()
        {
            _manager = LockDownManager.instance;

            if (_manager == null)
            {
                ABPrint.Print("Vous n'avez pas de LockDownManager !", ABColor.Red, ConsoleStyle.Bold);
                return;
            }
            
            ListenToEvent<OnLockDownStart_EVENT>(StartLockDown);
            ListenToEvent<OnLockDownEnd_EVENT>(EndLockDown);
        }

        private void StartLockDown(OnLockDownStart_EVENT data)
        {
            _lockDownUI.SetActive(true);
        }

        private void EndLockDown(OnLockDownEnd_EVENT data)
        {
            _lockDownUI.SetActive(false);
        }

        private void Update()
        {
            if (_manager.IsLockDown)
            {
                _lockDownCooldownImage.fillAmount = _manager.RatioDuration;
            }
        }
    }
}