using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class OpenDoorModule : ActionModule, IFirstAction
    {
        public Vector2Int Range => _canOpenAllDoors ? new Vector2Int(-1, -1) : _canOpenIndexRange;
        
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