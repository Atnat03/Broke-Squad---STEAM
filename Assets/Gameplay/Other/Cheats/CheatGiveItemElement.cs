using System;
using Bus;
using Gameplay.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.Other.Cheat
{
    public class CheatGiveItemElement : MonoBusListener
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _name;

        public void Initialise(SO_Item data, Action clickAction)
        {
            _image.sprite = data.Icon;
            _name.text = data.ItemName;
            
            _button.onClick.AddListener(() => clickAction());
        }
    }
}