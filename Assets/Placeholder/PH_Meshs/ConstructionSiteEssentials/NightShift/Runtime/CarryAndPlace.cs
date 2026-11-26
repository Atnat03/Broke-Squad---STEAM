using UnityEngine;

namespace ConstructionSiteEssentials.NightShift
{
    // The object and the socket use the same ground-level pivot.
    public sealed class CarryAndPlace : MonoBehaviour
    {
        [Min(0.05f)] public float snapRadius = 0.58f;
        [Min(0f)] public float carryHeight = 0.3f;
        public bool Held { get; private set; }
        public bool Placed { get; private set; }
        Vector3 home;
        Quaternion startingRotation;

        void Awake()
        {
            home = transform.position;
            startingRotation = transform.rotation;
        }

        public void PickUp()
        {
            if (Placed || Held) return;
            Held = true;
            transform.position += Vector3.up * carryHeight;
        }

        public void CarryTo(Vector3 groundPoint)
        {
            if (Held) transform.position = groundPoint + Vector3.up * carryHeight;
        }

        public bool TryPlace(Vector3 groundPoint, Transform socket)
        {
            if (!Held || socket == null) return false;
            Vector3 offset = groundPoint - socket.position;
            offset.y = 0;
            if (offset.sqrMagnitude > snapRadius * snapRadius) return false;
            transform.SetPositionAndRotation(socket.position, socket.rotation);
            Held = false;
            Placed = true;
            return true;
        }

        public void ResetItem()
        {
            transform.SetPositionAndRotation(home, startingRotation);
            Held = Placed = false;
        }
    }
}
