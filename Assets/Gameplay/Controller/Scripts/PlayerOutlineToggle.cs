using System;
using Network.Connections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerOutlineToggle : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Outline _outline;
        [SerializeField] private PlayerInput _playerInput;
        
        private Color _outlineColor;
        private static bool s_showOthers;
        private static event Action<bool> ShowOthersChanged;
        
        public event Action<bool> OnSeeThroughChanged;
        public bool IsSeeThrough { get; private set; }
        
        private readonly NetworkVariable<int> _colorIndex = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _colorIndex.OnValueChanged += OnColorIndexChanged;
            ApplyColor(_colorIndex.Value);

            if (IsOwner)
            {
                _colorIndex.Value = PlayerLocalData.instance.PlayerColor;
                ApplyColor(_colorIndex.Value);

                _outline.enabled = false; 

                _playerInput.OnStartShowOtherPlayer += OnStartShowOtherPlayer;
                _playerInput.OnEndShowOtherPlayer += OnEndShowOtherPlayer;
            }
            else
            {
                ShowOthersChanged += SetOutlineEnabled;
                SetOutlineEnabled(s_showOthers);
            }
        }

        public override void OnNetworkDespawn()
        {
            _colorIndex.OnValueChanged -= OnColorIndexChanged;

            if (IsOwner)
            {
                _playerInput.OnStartShowOtherPlayer -= OnStartShowOtherPlayer;
                _playerInput.OnEndShowOtherPlayer -= OnEndShowOtherPlayer;
                s_showOthers = false;
            }
            else
            {
                ShowOthersChanged -= SetOutlineEnabled;
            }

            base.OnNetworkDespawn();
        }

        private void OnStartShowOtherPlayer()
        {
            s_showOthers = true;
            ShowOthersChanged?.Invoke(true);
        }

        private void OnEndShowOtherPlayer()
        {
            s_showOthers = false;
            ShowOthersChanged?.Invoke(false);
        }

        private void SetOutlineEnabled(bool enabled)
        {
            _outline.enabled = enabled;
            IsSeeThrough = enabled;
            SetOutlineColor(enabled ? _outlineColor : Color.white);
            OnSeeThroughChanged?.Invoke(enabled);
        }

        private void OnColorIndexChanged(int previous, int current) => ApplyColor(current);

        private void ApplyColor(int index) => _outlineColor = PlayerLocalData.instance.PossibleColor[index];
        

        public void SetOutlineColor(Color color) => _outline.OutlineColor = color;
    }
}