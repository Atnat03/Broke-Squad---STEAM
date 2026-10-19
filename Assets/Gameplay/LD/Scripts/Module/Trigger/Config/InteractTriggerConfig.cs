using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public class InteractTriggerConfig : ModuleConfig
    {
        public float Range = 4f;
        public override Type ModuleType => typeof(InteractTrigger);
    }
}
