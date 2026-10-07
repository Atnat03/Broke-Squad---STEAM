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

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out INeedKey door))
            {
                if(door.CheckKey(_canOpenIndexRange))
                    ConsumeCondition(new OnModuleDoAction_EVENT());
            }
        }

        public void StopFirstAction()
        { }
    }
}