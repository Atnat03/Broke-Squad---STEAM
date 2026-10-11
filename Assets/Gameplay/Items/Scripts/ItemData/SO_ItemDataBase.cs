using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(fileName = "new Data Base", menuName = "Items/DataBase")]
    public class SO_ItemDataBase : ScriptableObject
    {
        [SerializeField] private List<SO_Item> _data = new List<SO_Item>();

        private void OnValidate()
        {
            ResetIdItemData();
        }

        private void ResetIdItemData()
        {
            for (int i = 0; i < _data.Count; i++)
            {
                _data[i].SetID(i);
            }
        }

        public int  Count => _data.Count;
        public SO_Item GetItemData(int index) => _data[index];
    }
}