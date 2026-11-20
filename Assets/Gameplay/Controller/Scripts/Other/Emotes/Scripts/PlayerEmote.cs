using System.Collections;
using Bus;
using Unity.Netcode;
using UnityEngine;

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
        [SerializeField] private GameObject _emoteCamera;
        private OnSetAnimationWillPlay_EVENT _emoteData = new OnSetAnimationWillPlay_EVENT{EmoteId = -1};
        private bool _isEmoting;
        private bool _wheelOpen;
        private Coroutine _emoteRoutine;
        
        public override void OnNetworkSpawn()
        {
            _playerInput.OnStartShowEmoteWheel += ShowEmoteWheel;
            _playerInput.OnEndShowEmoteWheel += HideEmoteWheel;
            _emoteCamera.SetActive(false);

            if (IsOwner)
                ListenToEvent<OnSetAnimationWillPlay_EVENT>(SetEmoteAnimation);
        }
    
        public override void OnNetworkDespawn()
        {
            _playerInput.OnStartShowEmoteWheel -= ShowEmoteWheel;
            _playerInput.OnEndShowEmoteWheel -= HideEmoteWheel;
        }

        private void ShowEmoteWheel()
        {
            if (!IsOwner || _isEmoting || _wheelOpen) return;
            if (_emoteRoutine != null) return;
            _wheelOpen = true;

            _playerController.SetInputEnabled(false);
            _playerController.PushLock();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            InvokeEvent(new OnEmoteWheelShow_EVENT { State = true });
        }
        
        private void HideEmoteWheel()
        {
            if (!IsOwner || !_wheelOpen) return;
            
            _wheelOpen = false;

            OnSetAnimationWillPlay_EVENT data = _emoteData;

            InvokeEvent(new OnEmoteWheelShow_EVENT { State = false });
            _playerController.PopLock();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (data.EmoteId > 0)
            {
                TryPlayEmote("Emote" + data.EmoteId);
                _emoteRoutine = StartCoroutine(CooldownActivateController(data.EmoteDuration));
                return;
            }

            ResetEmoteState();
        }

        private void ResetEmoteState()
        {
            _playerController.SetInputEnabled(true);
            _emoteData = new OnSetAnimationWillPlay_EVENT { EmoteId = 0 };
        }
        
        private void SetEmoteAnimation(OnSetAnimationWillPlay_EVENT data)
        {
            if (!IsOwner) return;
            
            _emoteData = data;
        }

        private IEnumerator CooldownActivateController(float dataAnimationDuration)
        {
            _isEmoting = true;
            _emoteCamera.SetActive(true);
            
            yield return new WaitForSeconds(dataAnimationDuration);

            _emoteRoutine = null;

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
            _emoteCamera.SetActive(false);
                    
            TryPlayEmote("StopEmote");
            
            ResetEmoteState();
        }
    }
}