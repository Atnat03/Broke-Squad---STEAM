using UnityEngine;

namespace Gameplay.Other.SuspiciousSound
{ 
    [CreateAssetMenu(menuName = "Suspicious Sound/Settings", fileName = "new Suspicious Sound Settings")]
    public class SO_SuspiciousSoundSettings : ScriptableObject
    {
        public float Force = 5;
        public float MaxDistance = 10;
    }
}