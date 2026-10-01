using System.Collections.Generic;
using Gameplay.Items.Scripts.ItemModules;
using Gameplay.Items.Scripts.PlayerItemGestion;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemData
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Item/ItemData", order = 0)]
    public class SO_Item : ScriptableObject
    {
        [SerializeField] public int id;
        [SerializeField] public string itemName;
        [SerializeField] public GameObject visualPrefab;
        [SerializeField] public ItemPickup pickUpPrefab;
        [SerializeField] public Sprite icon;
        [SerializeReference] public List<ILeftClick> leftClicksActions;
        [SerializeReference] public List<IRightClick> rightClicksActions;
        [SerializeReference] public List<ICondition> conditions;
        [SerializeReference] public List<IPassif> passif;
    }
}