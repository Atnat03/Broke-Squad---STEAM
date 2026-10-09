using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    [CreateAssetMenu(fileName = "LDElementRegistry", menuName = "LD/New Element Registry")]
    public class SO_LDElementRegistry : ScriptableObject
    {
        public List<SO_LDElementData> LDElementsList;
    }
}
