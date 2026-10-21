using Network.Connections;
using Network.HUB;
using UnityEngine;
using UnityEngine.UI;

public class SelectColorPrefab : MonoBehaviour
{
    [SerializeField] private Image _colorImage;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Button _buttonSelected;

    public void SetUpColor(int color, PlayerLocalPersonalisation manager)
    {
        _colorImage.color = PlayerLocalData.instance.PossibleColor[color];
        
        _buttonSelected.onClick.AddListener(() =>
        {
            manager.DisableAllSelectionUI();
            _selectedImage.gameObject.SetActive(true);
            
            PlayerLocalData.instance.PlayerColor = color;
        });
    }

    public void AddClickChangeColor(LobbyManager man, int id)
    {
        _buttonSelected.onClick.AddListener(() =>
        {
            man.SetColorButton(id);
        });
    }

    public void DisableSelection() => _selectedImage.gameObject.SetActive(false);
}
