using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Gameplay.LD
{
    [CreateAssetMenu(fileName = "LDElementRegistry", menuName = "LD/New Element Registry")]
    public class LDElementRegistry : ScriptableObject
    {
        public List<LDElementData> LDElementsList;
    }
}
