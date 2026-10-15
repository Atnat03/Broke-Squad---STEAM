using System;
using UnityEngine;

namespace Gameplay.LD
{
    [Serializable]
    public class KeyConditionConfig : ModuleConfig 
    {
        public bool CanOpenWithoutKey = false;
        public int IdKey = 0;
        
        public override Type ModuleType => typeof(KeyConditionConfig);
    }
}