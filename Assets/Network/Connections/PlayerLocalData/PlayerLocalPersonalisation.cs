using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Network.Connections
{
    public class PlayerLocalPersonalisation : MonoBehaviour
    {
        [Header("Pseudo")]
        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private TMP_InputField _playerInputField;
        
        [Header("Color")]
        [SerializeField] private Transform _parentSpawn;
        [SerializeField] private SelectColorPrefab _selectColorPrefab;
        List<SelectColorPrefab> _selectColorList = new();

        void Start()
        {
            for(int i = 0; i < PlayerLocalData.instance.PossibleColor.Length; i++)
            {
                SelectColorPrefab colorInstance = Instantiate(_selectColorPrefab, _parentSpawn);
                colorInstance.SetUpColor(i, this);
                _selectColorList.Add(colorInstance);
            }

            DisableAllSelectionUI();
        }
        
        public void DisableAllSelectionUI()
        {
            foreach (SelectColorPrefab c in _selectColorList)
            {
                c.DisableSelection();
            }
        }
        
        public void ChangeName()
        {
            PlayerLocalData.instance.PlayerName = _playerInputField.text;
            _playerName.text = _playerInputField.text;
        }
    }
}