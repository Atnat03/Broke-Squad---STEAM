using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class OpenDoorModule : ActionModule, IFirstAction
    {
        [SerializeField] private Vector2Int _canOpenIndexRange = new Vector2Int(0, 2);
        [SerializeField] private bool _canOpenAllDoors = false;
        
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            Transform cam = Context.Camera.transform;
        }

        public void StopFirstAction()
        { }
    }
}