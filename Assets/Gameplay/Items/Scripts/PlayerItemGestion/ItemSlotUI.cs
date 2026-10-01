using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Items.Scripts.PlayerItemGestion
{
    public class ItemSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _selectedImage;

        public void SetIcon(Sprite sprite)
        {
            _icon.sprite = sprite;
        }

        public void SetSelectedImage(bool selected) => _selectedImage.gameObject.SetActive(selected);
    }
}