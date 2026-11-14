using System;
using Network.Connections;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    public class OutlinePlayerToggle : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Outline _outline;
        [SerializeField] private PlayerInput _playerInput;
        
        private static bool s_showOthers;
        private static event Action<bool> ShowOthersChanged;
        
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

        private void SetOutlineEnabled(bool enabled) => _outline.enabled = enabled;

        private void OnColorIndexChanged(int previous, int current) => ApplyColor(current);

        private void ApplyColor(int index) =>
            SetOutlineColor(PlayerLocalData.instance.PossibleColor[index]);

        public void SetOutlineColor(Color color) => _outline.OutlineColor = color;
    }
}