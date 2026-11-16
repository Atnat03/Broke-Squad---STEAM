using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Items
{
    public class DragHandle : MonoBehaviour, IHoldInteractable
    {
        [SerializeField] private Outline _outline;
        [SerializeField] private Transform snapPoint; 
        [SerializeField, Min(0)] private int axis; 

        private HeavyDraggable _owner;
        private int _index;

        public int Axis => axis;
        public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
        public Outline outline => _outline;

        public void Init(HeavyDraggable owner, int index)
        {
            _owner = owner;
            _index = index;
        }

        public string GetTooltip(ItemModule[] playerModules) => _owner.IsHandleFree(_index) ? "Hold to drag" : "Already taken";

        public void SetOutline(bool state)
        {
            if (_outline != null) _outline.enabled = state;
        }

        public void Interact(ulong clientId) { }

        public void HoldStart(ulong clientId) => _owner.RequestHold(_index);
        public void HoldEnd(ulong clientId) => _owner.RequestRelease(_index);
    }
}