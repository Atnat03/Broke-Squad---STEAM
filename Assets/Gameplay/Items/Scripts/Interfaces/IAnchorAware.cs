using UnityEngine;

namespace Gameplay.Items
{
    public interface IAnchorAware
    {
        void SetAnchor(Transform anchor, ulong hostId);
    }
}