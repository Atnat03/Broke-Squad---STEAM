using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LD
{
    [CreateAssetMenu(fileName = "LDElementData", menuName = "LD/New Element Data")]
    public class LDElementData : ScriptableObject
    {
        public GameObject Prefab;
        public List<LDBindingData> BindingsList = new();
        public bool initialFlag = true;
        [SerializeReference] public List<ModuleConfig> ModulesList = new();
    }
}