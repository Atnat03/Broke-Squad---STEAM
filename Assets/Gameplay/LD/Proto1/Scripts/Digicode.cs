using System;
using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD.Proto1
{
    public class Digicode : NetworkBehaviour
    {
        public enum DisplayState : byte { Normal, Wrong, Success }
        
        [Header("Code")]
        [SerializeField] private string code = "1234";
        [SerializeField] private float openDuration = 0f;
        [SerializeField] private float wrongCooldown = 2f;
        
        [Header("KeyPad View")]
        [SerializeField] private TextMeshProUGUI display;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color wrongColor = Color.red;
        [SerializeField] Color successColor = Color.green;
        
        [Header("Sound")]
        [SerializeField, SoundName] private string clickSound;
        [SerializeField, SoundName] private string successSound;
        [SerializeField, SoundName] private string wrongSound;
        
        
        public NetworkVariable<bool> unlocked = new(false);
        private readonly NetworkVariable<FixedString32Bytes> _buffer = new(default);
        private readonly NetworkVariable<DisplayState> _displayState = new(DisplayState.Normal);
        
        private Coroutine _relockRoutine;
        private Coroutine _wrongRoutine;
        public event Action StateChanged;
        
        public override void OnNetworkSpawn()
        {
            _buffer.OnValueChanged += OnBufferChanged;
            _displayState.OnValueChanged += OnDisplayStateChanged;
            unlocked.OnValueChanged += OnUnlockedChanged;
            
            RefreshDisplay();
        }

        public override void OnNetworkDespawn()
        {
            _buffer.OnValueChanged -= OnBufferChanged;
            _displayState.OnValueChanged -= OnDisplayStateChanged;
            unlocked.OnValueChanged -= OnUnlockedChanged;
        }

        #region ClientSide

        public void PressDigit(int digit)
        {
            if (unlocked.Value) return;
            if (digit < 0 || digit > 9) return;
            PressDigitServerRpc(digit);
        }

        public void PressClear()
        {
            if (unlocked.Value) return;
            PressClearServerRpc();
        }

        public void PressConfirm()
        {
            if (unlocked.Value) return;
            PressConfirmServerRpc();
        }

        #endregion
        

        #region ServerLogic

        [Rpc(SendTo.Server)]
        private void PressDigitServerRpc(int digit)
        {
            if (!CanAcceptInput()) return;
            if (digit < 0 || digit > 9) return;
            if (_buffer.Value.Length >= code.Length) return;

            _buffer.Value = new FixedString32Bytes(_buffer.Value.ToString() + digit);
        }

        [Rpc(SendTo.Server)]
        private void PressClearServerRpc()
        {
            if (!CanAcceptInput()) return;
            _buffer.Value = default;
        }

        [Rpc(SendTo.Server)]
        private void PressConfirmServerRpc()
        {
            if (!CanAcceptInput()) return;
            if (_buffer.Value.Length == 0) return;

            if (_buffer.Value.ToString() == code)
            {
                Unlock();
            }
            else
            {
                OnWrongCode();
            }
                
        }

        private bool CanAcceptInput()
        {
            return !unlocked.Value && _displayState.Value == DisplayState.Normal;
        }

        private void Unlock()
        {
            _displayState.Value = DisplayState.Success;
            unlocked.Value = true;

            if (_relockRoutine != null) StopCoroutine(_relockRoutine);
            if (openDuration > 0f) _relockRoutine = StartCoroutine(Relock());
        }

        private IEnumerator Relock()
        {
            yield return new WaitForSeconds(openDuration);
            unlocked.Value = false;
            _buffer.Value = default;
            _displayState.Value = DisplayState.Normal;
            _relockRoutine = null;
        }

        private void OnWrongCode()
        {
            _displayState.Value = DisplayState.Wrong;

            if (_wrongRoutine != null) StopCoroutine(_wrongRoutine);
            _wrongRoutine = StartCoroutine(WrongCooldown());
        }

        private IEnumerator WrongCooldown()
        {
            yield return new WaitForSeconds(wrongCooldown);
            _buffer.Value = default;
            _displayState.Value = DisplayState.Normal;
            _wrongRoutine = null;
        }

        #endregion
        

        #region ReplicationCallback

        private void OnBufferChanged(FixedString32Bytes previous, FixedString32Bytes current)
        {
            RefreshDisplay();
            
        }

        private void OnDisplayStateChanged(DisplayState previous, DisplayState current)
        {
            RefreshDisplay();
        }

        private void OnUnlockedChanged(bool previous, bool current)
        {
            StateChanged?.Invoke(); 
        }

        private void RefreshDisplay()
        {
            if (display == null) return;

            display.text = _buffer.Value.ToString();
            display.color = _displayState.Value switch
            {
                DisplayState.Wrong => wrongColor,
                DisplayState.Success => successColor,
                _ => normalColor
            };
        }

        #endregion

        

    }
}