using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(fileName = "new ItemData", menuName = "Items/ItemData")]
    public class SO_Item : ScriptableObject
    {
        [HideInInspector] public int ID;
        [SerializeField] public string ItemName;
        [SerializeField] public GameObject VisualPrefabLocal;
        [SerializeField] public GameObject VisualPrefabRemote;
        [SerializeField] public ItemPickup PickupPrefab;
        [SerializeField] public Sprite Icon;
        [SerializeField] public Vector3 OffsetInHand = new Vector3(0, 0, 0);
        [SerializeField] public TooltipConfig TooltipInHand;
        [SerializeField] public float Weight = 1;
        [SerializeReference] public List<IFirstAction> FirstActionList;
        [SerializeReference] public List<ISecondAction> SecondActionList;
        [SerializeReference] public List<IPassif> PassifList;
        
        public void SetID(int id) => ID = id;
    }
}