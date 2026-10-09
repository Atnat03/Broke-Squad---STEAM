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
    public class LDBindingData
    {
        public int TriggerID;
        public BindingAction Action = BindingAction.Start;

        [Tooltip("0 = immédiat")]
        [Min(0f)] public float Delay;

        [Tooltip("Annule les délais en attente de l'élément (ex : désamorcer avant que le piège frappe).")]
        public bool CancelsPendingDelays;

        public List<int> ConditionIDs = new();
        public List<int> EffectIDs = new();
    }
}
