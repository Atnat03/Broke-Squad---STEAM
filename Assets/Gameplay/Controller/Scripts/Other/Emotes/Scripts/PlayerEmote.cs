using System.Collections;
using Bus;
using Gameplay.Controller;
using MyPrint;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Gameplay.Controller.Emote
{
    public struct OnEmoteWheelShow_EVENT
    {
        public bool State;
    }

    public class PlayerEmote : NetworkBusListener
    {
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Animator _animator;
        [SerializeField] private Camera _emoteCamera;

        public override void OnNetworkSpawn()
        {
            _playerInput.OnStartShowEmoteWheel += ShowEmoteWheel;
            _playerInput.OnEndShowEmoteWheel += HideEmoteWheel;

            _emoteCamera.gameObject.SetActive(false);
            
            if (IsOwner)
            {
                ListenToEvent<OnPlayEmote_EVENT>(PlayEmote);
            }
        }

        public override void OnNetworkDespawn()
        {
            _playerInput.OnStartShowEmoteWheel -= ShowEmoteWheel;
            _playerInput.OnEndShowEmoteWheel -= HideEmoteWheel;
        }

        private void ShowEmoteWheel()
        {
            if(!IsOwner) return;
            
            _playerController.PushLock();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            InvokeEvent(new OnEmoteWheelShow_EVENT{State = true});
        }
        
        private void HideEmoteWheel()
        {
            if(!IsOwner) return;
            
            _playerController.PopLock();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            InvokeEvent(new OnEmoteWheelShow_EVENT{State = false});
        }
        
        private void PlayEmote(OnPlayEmote_EVENT data)
        {
            if (!IsOwner) return;
            
            _playerController.PushLock();

            StartCoroutine(CooldownActivateController(data.AnimationDuration));
            
            TryPlayEmote(data.TriggerAnimationName);
        }

        private IEnumerator CooldownActivateController(float dataAnimationDuration)
        {
            _emoteCamera.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(dataAnimationDuration);
            
            _emoteCamera.gameObject.SetActive(false);
            
            _playerController.PopLock();
        }

        private void TryPlayEmote(string s)
        {
            if (!IsServer)
            {
                PlayEmoteServerRpc(s);
                return;
            }

            PlayEmoteClientRpc(s);
        }
        
        [Rpc(SendTo.Server)]
        private void PlayEmoteServerRpc(string s) => TryPlayEmote(s);
        
        [Rpc(SendTo.Everyone)]
        private void PlayEmoteClientRpc(string s)
        {
            _animator.SetTrigger(s);
        }
    }
}