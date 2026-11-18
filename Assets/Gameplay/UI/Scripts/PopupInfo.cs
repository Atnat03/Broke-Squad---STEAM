using System.Collections;
using Bus;
using Gameplay.Items;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.UI.Scripts
{
    
    public class PopupInfo : MonoBusListener
    {
        [SerializeField] private string _keyEvent = "POPUP_INFO";

        [Header("Settings")] 
        [SerializeField] private float _popupDuration = 2;
        private bool _canPopup = true;

        [Header("UI")] 
        [SerializeField] private GameObject _popUpPrefab;
        
        void OnEnable()
        {
            ListenToEvent<OnModuleDoAction_EVENT>(CreatePopUp);
        }

        private void CreatePopUp(OnModuleDoAction_EVENT data)
        {
            if (data.KeyEvent != _keyEvent) return;
            if (!_canPopup) return;
            
            _canPopup = false;

            Destroy(Instantiate(_popUpPrefab, transform), _popupDuration);

            StartCoroutine(PopupCooldown());
        }

        IEnumerator PopupCooldown()
        {
            yield return new WaitForSeconds(_popupDuration);

            _canPopup = true;
        }
    }
}