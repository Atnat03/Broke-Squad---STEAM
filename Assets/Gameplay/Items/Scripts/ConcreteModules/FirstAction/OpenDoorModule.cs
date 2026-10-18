using System;
using MyPrint;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class OpenDoorModule : ActionModule, IFirstAction, IServerAction
    {
        public Vector2Int Range => _canOpenAllDoors ? new Vector2Int(-1, -1) : _canOpenIndexRange;
        
        [SerializeField] private Vector2Int _canOpenIndexRange = new Vector2Int(0, 2);
        [SerializeField] private bool _canOpenAllDoors = false;
        
        public ICondition Condition => ConditionParent;
        
        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            Transform cam = Context.Camera.transform;

            Context.SendToServer(this, new OnModuleDoAction_EVENT
            {
                Position =  cam.position,
                Direction = cam.forward,
            });
        }

        public void StopFirstAction()
        { }

        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            Vector3 origin = data.Position;
            Vector3 direction = data.Direction;
            
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out Door door))
            {
                bool b = door.TryUnlock(Range);
                ABPrint.PrintBool(b);
                
                if(b)
                {
                    ConsumeCondition(new OnModuleDoAction_EVENT());
                }
            }
        }
    }
}