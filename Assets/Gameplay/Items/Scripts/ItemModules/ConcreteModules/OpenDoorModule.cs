using System;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class OpenDoorModule : ItemModule, ILeftClick
    {
        [SerializeField] private int _doorId;
        [SerializeField] private float _range = 2f;
        [SerializeField] private bool _openAllDoors = false;

        public float Range => _range;

        public void StartLeftClick()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _range)
                && hit.transform.TryGetComponent(out Door door)
                && door.TryGetComponent(out NetworkObject netObj))
            {
                Context.Inventory.RequestOpenDoor(netObj);
            }
        }

        public void EndLeftClick() { }

        public bool ApplyOpen(Door door)
        {
            int id = _openAllDoors ? -1 : _doorId;
            
            return door.TryOpen(id);
        }
    }
}