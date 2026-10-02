using System;
using Gameplay.LD.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items.Scripts.ItemModules.ConcreteModules
{
    [Serializable]
    public class OpenDoorModule : ItemModule, ILeftClick
    {
        [SerializeField, MinMaxSlider(0, 500)] private Vector2Int _doorIdRange = new Vector2Int(0, 100);
        [SerializeField] private float _range = 2f;
        [SerializeField] private bool _openAllDoors = false;
        [SerializeField, SoundName] private string _useKeySound;

        public float Range => _range;

        public void StartLeftClick()
        {
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, _range)
                && hit.transform.TryGetComponent(out Door door)
                && door.TryGetComponent(out NetworkObject netObj))
            {
                Context.Inventory.TryPlaySound(_useKeySound);
                Context.Inventory.RequestOpenDoor(netObj);
                Context.Core.UseCondition();
            }
        }

        public void EndLeftClick() { }

        public bool ApplyOpen(Door door)
        {
            Vector2Int id = _openAllDoors ? new Vector2Int(-1, -1) : _doorIdRange;
            
            return door.TryOpen(id);
        }
    }
}