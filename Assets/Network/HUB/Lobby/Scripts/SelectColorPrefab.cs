using System;
using Network.Connections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelectColorPrefab : MonoBehaviour
{
    [SerializeField] private Image _colorImage;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Button _buttonSelected;

    public void SetUpColor(Color color, PlayerLocalPersonalisation manager)
    {
        _colorImage.color = color;
        
        _buttonSelected.onClick.AddListener(() =>
        {
            manager.DisableAllSelectionUI();
            _selectedImage.gameObject.SetActive(true);
            
            PlayerLocalData.instance.PlayerColor = color;
        });
    }

    public void DisableSelection() => _selectedImage.gameObject.SetActive(false);
}
