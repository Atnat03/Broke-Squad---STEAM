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
        
        [Header("SFX")] 
        [SerializeField, SoundName] private string _stringNameGoodKey;
        [SerializeField, SoundName] private string _stringNameWringKey;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;

        
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
                    //SFX
                    Context.Inventory.InvokeEvent(new PlaySoundEvent
                    {
                        position = origin,
                        volume = _volume,
                        soundName = _stringNameGoodKey
                    });
                    
                    ConsumeCondition(new OnModuleDoAction_EVENT());
                }
                else
                {
                    //SFX
                    Context.Inventory.InvokeEvent(new PlaySoundEvent
                    {
                        position = origin,
                        volume = _volume,
                        soundName = _stringNameWringKey
                    });
                }
            }
        }
    }
}