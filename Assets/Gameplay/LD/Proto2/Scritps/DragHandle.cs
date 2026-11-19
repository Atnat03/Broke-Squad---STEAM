using Gameplay.Controller;
using Gameplay.Items;
using UnityEngine;

namespace Gameplay.Items
{
    public class DragHandle : MonoBehaviour, IHoldInteractable
    {
        [SerializeField] private Outline _outline;
        [SerializeField] private Transform snapPoint;
        [SerializeField, Min(0)] private int axis;

        [Header("Space check")]
        [SerializeField] private LayerMask blockingMask = ~0;
        [SerializeField] private float playerRadius = 0.35f;
        [SerializeField] private float playerHeight = 1.8f;
        [SerializeField] private float skin = 0.05f;
        

        private static readonly Collider[] Hits = new Collider[16];

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

        public bool HasSpace()
        {
            Vector3 p = SnapPoint.position;
            float r = Mathf.Max(0.01f, playerRadius - skin);
            Vector3 bottom = p + Vector3.up * (r + skin);
            Vector3 top = p + Vector3.up * Mathf.Max(r + skin, playerHeight - r);

            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, Hits, blockingMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < n; i++)
            {
                Collider c = Hits[i];
                if (c.transform.IsChildOf(_owner.transform)) continue;       
                return false;
            }
            return true;
        }

        
        public string GetTooltip(ItemModule[] playerModules)
        {
            if (!_owner.IsHandleFree(_index)) return "Already taken";
            if (!HasSpace()) return "Not enough space";
            return "Hold to drag";
        }

        public void SetOutline(StartOutliningData data)
        {
            if (_outline != null) _outline.enabled = data.State;
        }

        public void Interact(ulong clientId) { }

        public void HoldStart(ulong clientId)
        {
            if (!_owner.IsHandleFree(_index) || !HasSpace()) return;
            _owner.RequestHold(_index);
        }

        public void HoldEnd(ulong clientId) => _owner.RequestRelease(_index);
    }
}