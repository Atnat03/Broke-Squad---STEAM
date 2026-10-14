using UnityEngine;

namespace Gameplay.Controller
{
    public struct CameraPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
    }

    public interface ICameraOverride
    {
        bool LocksController { get; }
        bool IsFinished { get; }                        

        void OnEnter(ref float yaw, ref float pitch);    
        void ProcessLook(ref float yaw, ref float pitch);
        void Tick(float dt);                            
        CameraPose Apply(in CameraPose normal);         
        void Release();                                
    }
}