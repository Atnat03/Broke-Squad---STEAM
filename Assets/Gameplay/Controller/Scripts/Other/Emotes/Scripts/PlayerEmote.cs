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
        private OnSetAnimationWillPlay_EVENT _emoteData = new OnSetAnimationWillPlay_EVENT{EmoteId = -1};
        private bool _isEmoting;

        public override void OnNetworkSpawn()
        {
            _playerInput.OnStartShowEmoteWheel += ShowEmoteWheel;
            _playerInput.OnEndShowEmoteWheel += HideEmoteWheel;

            _playerInput.OnMoveInput += CancelEmote;

            _emoteCamera.gameObject.SetActive(false);
            
            if (IsOwner)
            {
                ListenToEvent<OnSetAnimationWillPlay_EVENT>(SetEmoteAnimation);
            }
        }

        public override void OnNetworkDespawn()
        {
            _playerInput.OnStartShowEmoteWheel -= ShowEmoteWheel;
            _playerInput.OnEndShowEmoteWheel -= HideEmoteWheel;
            _playerInput.OnMoveInput -= CancelEmote;
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

            if (_emoteData.EmoteId > 0)
            {
                ABPrint.Print("Emote : " + _emoteData.EmoteDuration, ABColor.White);
                
                TryPlayEmote("Emote" + _emoteData.EmoteId);
                
                StartCoroutine(CooldownActivateController(_emoteData.EmoteDuration));
            }

            _playerController.PopLock();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _emoteData = new OnSetAnimationWillPlay_EVENT { EmoteId = 0 };
            
            InvokeEvent(new OnEmoteWheelShow_EVENT{State = false});
        }
        
        private void SetEmoteAnimation(OnSetAnimationWillPlay_EVENT data)
        {
            if (!IsOwner) return;
            
            _emoteData = data;
        }

        private IEnumerator CooldownActivateController(float dataAnimationDuration)
        {
            _isEmoting = true;
            _emoteCamera.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(dataAnimationDuration);

            StopEmoting();
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

        private void StopEmoting()
        {
            _isEmoting = false;
            _emoteCamera.gameObject.SetActive(false);
                    
            TryPlayEmote("StopEmote");
        }
        
        private void CancelEmote(Vector2 obj)
        {
            if (_isEmoting)
            {
                if (obj.magnitude > 0.1f)
                {
                    StopAllCoroutines();
                    StopEmoting();
                }
            }
        }
    }
}