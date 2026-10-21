using System;
using System.Collections;
using System.Collections.Generic;
using Network.HUB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Network.Connections
{
    public class PlayerLocalPersonalisation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LobbyManager _lobbyManager;
        
        [Header("Pseudo")]
        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private TMP_InputField _playerInputField;
        [SerializeField] private Image _confirmNameButtonImage;
        [SerializeField] private Color[] _confirmColor;
        private string _currentName = String.Empty;
        
        [Header("Color")]
        [SerializeField] private Transform _parentSpawn;
        [SerializeField] private SelectColorPrefab _selectColorPrefab;
        List<SelectColorPrefab> _selectColorList = new();
        
        [Header("Ready")]
        [SerializeField] private Image _readyButton;
        [SerializeField] private TextMeshProUGUI _readyText;
        private bool _isReady = false;
        private bool _canSetReady = true;

        void Start()
        {
            for(int i = 0; i < PlayerLocalData.instance.PossibleColor.Length; i++)
            {
                SelectColorPrefab colorInstance = Instantiate(_selectColorPrefab, _parentSpawn);
                colorInstance.SetUpColor(i, this);
                colorInstance.AddClickChangeColor(_lobbyManager, i);
                _selectColorList.Add(colorInstance);
            }
            
            _confirmNameButtonImage.color = _confirmColor[1];
            _readyButton.color = _confirmColor[1];

            DisableAllSelectionUI();
        }
        
        public void DisableAllSelectionUI()
        {
            foreach (SelectColorPrefab c in _selectColorList)
            {
                c.DisableSelection();
            }
        }

        private void Update()
        {
            if (_currentName.Equals(String.Empty)) return;
            
            if (!_currentName.Equals(_playerInputField.text))
            {
                _confirmNameButtonImage.color = _confirmColor[1];
            }
        }

        public void ChangeName()
        {
            string newName = _playerInputField.text;
            
            PlayerLocalData.instance.PlayerName = newName;
            _lobbyManager.SetNameButton(newName);
            _playerName.text = newName;
            _currentName = newName;
            
            _confirmNameButtonImage.color = _confirmColor[0];
        }

        public void SetReadyButton()
        {
            if (!_canSetReady) return;
            
            _isReady = !_isReady;
            _readyButton.color = _isReady ?  _confirmColor[0] : _confirmColor[1];
            _readyText.text = _isReady ?  "READY" : "NOT READY";

            StartCoroutine(ReadyBuffer());
        }

        IEnumerator ReadyBuffer()
        {
            _canSetReady = false;
            
            yield return new WaitForSeconds(1);
            
            _canSetReady = true;
        }
    }
}