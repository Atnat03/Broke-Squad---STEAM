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
        
        [Header("Color")]
        [SerializeField] private Color[] _colors;
        [SerializeField] private Transform _parentSpawn;
        [SerializeField] private SelectColorPrefab _selectColorPrefab;
        List<SelectColorPrefab> _selectColorList = new();

        void Start()
        {
            foreach (var color in _colors)
            {
                SelectColorPrefab colorInstance = Instantiate(_selectColorPrefab, _parentSpawn);
                colorInstance.SetUpColor(color, this);
                _selectColorList.Add(colorInstance);
            }
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
            PlayerLocalData.instance.PlayerName = _playerName.text;
        }
    }
}