using UnityEngine;

namespace VFX_Manage
{
    public struct PlayVFX_EVENT
    {
        public string vfxName;
        public Vector3 position;
        public Quaternion rotation;
        public float scale;

        public bool shared;
    }
}