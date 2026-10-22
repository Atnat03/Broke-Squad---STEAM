using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(fileName = "new Data Base", menuName = "Items/DataBase")]
    public class SO_ItemDataBase : ScriptableObject
    {
        [SerializeField] public List<SO_Item> Data = new List<SO_Item>();

        private void OnValidate()
        {
            ResetIdItemData();
        }

        private void ResetIdItemData()
        {
            for (int i = 0; i < Data.Count; i++)
            {
                Data[i].SetID(i);
            }
        }

        public int  Count => Data.Count;
        public SO_Item GetItemData(int index) => Data[index];
    }
}