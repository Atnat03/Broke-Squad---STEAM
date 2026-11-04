using System.Collections.Generic;
using System.Linq;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.IA.Refacto
{
    public class GuardOpenDoor : NetworkBehaviour
    {
        [SerializeField] private float _distanceFromDoorToCloseIt = 2.5f;
        
        private List<Door> _doorList = new();
        
        public void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            
            if (other.transform.TryGetComponent(out Door door))
            {
                if (door.IsLocked) return;

                if (door.IsOpen) return;

                ABPrint.Print("Open a door");
                door.TryOpenDoor();
                
                _doorList.Add(door);
            }
        }

        private void Update()
        {
            foreach (var door in _doorList.ToList())
            {
                float distance = (transform.position - door.transform.position).sqrMagnitude;

                ABPrint.Print("Distance: " + distance);
                
                if (distance > _distanceFromDoorToCloseIt)
                {
                    ABPrint.Print("Close a door");
                    door.TryCloseDoor();
                    _doorList.Remove(door);
                }
            }
        }
    }
}