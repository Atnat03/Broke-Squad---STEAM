using UnityEngine;

namespace Gameplay.Other.SuspiciousSound
{
    public struct OnCreateSuspiciousSound_EVENT
    {
        public ulong FromClientID;
        public float Force;
        public float MaxDistance;
        public Vector3 Position;
    }
}