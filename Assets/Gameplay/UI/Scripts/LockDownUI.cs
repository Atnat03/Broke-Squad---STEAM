using System;
using Bus;
using Gameplay.Controller;
using Gameplay.IA;
using MyPrint;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class LockDownUI : MonoBusListener
    {
        [Header("UI")]
        [SerializeField] private GameObject _lockDownUI;
        [SerializeField] private Image _lockDownCooldownImage;
        
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _lockDownTriggerPlayer;
        [SerializeField] private string _triggerPlayerMessage = " a été repéré ! Le bâtiment est en état d'alerte !";
        [SerializeField] private string _triggerPlayerMessageGoal = " a le tableau, COURREZ !!";
        
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
            
            string playerName = data.PlayerTriggerName.Value;

            _lockDownTriggerPlayer.text = playerName + (data.isGoal ? _triggerPlayerMessageGoal :_triggerPlayerMessage);
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