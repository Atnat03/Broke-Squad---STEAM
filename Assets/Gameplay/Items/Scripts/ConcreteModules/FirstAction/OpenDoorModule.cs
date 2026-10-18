using System;
using Gameplay.LD.Proto1.Scripts;
using MyPrint;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

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

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out Door door))
            {
                if(door.TryUnlock(Range))
                    ConsumeCondition(new OnModuleDoAction_EVENT());
            }
        }

        public void StopFirstAction()
        { }
    }
}