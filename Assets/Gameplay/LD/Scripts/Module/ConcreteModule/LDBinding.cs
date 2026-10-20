using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.LD
{
    public enum BindingAction
    {
        Start,
        Stop
    }

    [Serializable]
    public class LDBinding
    {
        [Tooltip("Qu'est ce qui déclenche")]
        public LDTriggerModule Trigger;
        [Tooltip("Start : lance les effets / stop : les arrêtes")]
        public BindingAction Action = BindingAction.Start;
        [Min(0f)] public float Delay;
        public bool CancelsPendingDelays;
        [Tooltip("Liste des conditions")]
        public LDConditionModule[] Conditions;
        [Tooltip("Liste des effets")]
        public LDEffectModule[] Effects;
    }
}
