using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class InteractTriggerConfig : ModuleConfig
    {
        public Vector3 Center = Vector3.zero;
        public Vector3 Size = Vector3.one;
        public float Range = 4f;
        public override Type ModuleType => typeof(InteractTrigger);
    }
}
