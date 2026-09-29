using System.Collections.Generic;
using Gameplay.Items.Scripts.ItemModules;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemData
{
    [CreateAssetMenu(fileName = "Item/ItemData", menuName = "ItemData", order = 1)]
    public class SO_Item : ScriptableObject
    {
        public int ID => _id;
        public GameObject Visual => _visualPrefab;
        
        [SerializeField] private int _id;
        [SerializeField] private string _name;
        [SerializeField] private GameObject _visualPrefab;
        [SerializeReference] private List<ILeftClick> _leftClicksActions;
        [SerializeReference] private List<IRightClick> _rightClicksActions;
        [SerializeReference] private List<ICondition> _conditions;
        [SerializeReference] private List<IPassif> _passif;
    }
}