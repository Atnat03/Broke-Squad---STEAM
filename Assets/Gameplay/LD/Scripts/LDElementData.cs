using System.Collections.Generic;
using UnityEngine;

namespace Assets.Gameplay.LD
{
    [CreateAssetMenu(fileName = "LDElementData", menuName = "LD/New Element Data")]
    public class LDElementData : ScriptableObject
    {
        public string Name;
        public GameObject Prefab;
        public List<LDBindingData> BindingsList;
        public List<ModuleConfig> ModulesList;
    }
}