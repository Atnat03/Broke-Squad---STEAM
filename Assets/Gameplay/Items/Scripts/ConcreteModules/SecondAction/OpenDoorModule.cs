using System;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class OpenDoorModule : ItemModule, IFirstAction
    {
        [SerializeField] private Vector2Int _canOpenIndexRange = new Vector2Int(0, 2);
        [SerializeField] private bool _canOpenAllDoors = false;
        
        public ICondition Condition { get; }
        
        public void StartFirstAction()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f) && hit.transform.TryGetComponent(out INeedKey door))
            {
                door.CheckKey(_canOpenIndexRange);
            }
        }

        public void StopFirstAction()
        { }
    }
}