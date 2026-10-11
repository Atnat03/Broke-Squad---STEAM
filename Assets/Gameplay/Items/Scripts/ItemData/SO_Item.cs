using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(fileName = "new ItemData", menuName = "Items/ItemData")]
    public class SO_Item : ScriptableObject
    {
        [HideInInspector] public int ID;
        [SerializeField] public string ItemName;
        [SerializeField] public GameObject VisualPrefab;
        [SerializeField] public ItemPickup PickupPrefab;
        [SerializeField] public Sprite Icon;
        [SerializeReference] public List<IFirstAction> FirstActionList;
        [SerializeReference] public List<ISecondAction> SecondActionList;
        [SerializeReference] public List<IPassif> PassifList;
        
        public void SetID(int id) => ID = id;
    }
}