using System;
using UnityEngine;

namespace Gameplay.LD
{
    [Serializable]
    public class PlayerEnterTriggerConfig : ModuleConfig
    {
        public Vector3 Center = Vector3.zero;
        public Vector3 Size = new Vector3(1f, 1f, 1f);

        public override Type ModuleType => typeof(PlayerEnterTrigger);
    }
}
