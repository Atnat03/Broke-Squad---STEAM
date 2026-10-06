using System;
using UnityEngine;

namespace Gameplay.Controller
{
    [Serializable]
    public class GroundChecker
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float checkDist = 0.15f;
        [SerializeField] private float radiusMultiplier = 0.9f;
        [SerializeField] private float skin = 0.1f;
        [SerializeField] private float maxSlopeAngle = 50f;
        
        private readonly RaycastHit[] _hits = new RaycastHit[10];
        
        public bool IsGrounded { get; private set; }
        public Vector3 Normal { get; private set; } = Vector3.up;
        
        public void ForceAirborne() => IsGrounded = false;

        public void Check(PlayerBody body, Rigidbody rb, Transform root)
        {
            float radius = body.WorldRadius * radiusMultiplier;
            Vector3 origin = body.FeetPosition + Vector3.up * (radius + skin);
            float dist = skin + checkDist;
            
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, _hits, dist,
                groundLayer, QueryTriggerInteraction.Ignore);

            bool hitGround = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(root)) continue;

                if (Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle)
                {
                    Normal = hit.normal;
                    hitGround = true;
                    break;
                }
            }
            
            IsGrounded = hitGround && rb.linearVelocity.y < 0.1f;
            Debug.DrawRay(origin, Vector3.down * dist, IsGrounded ? Color.green : Color.red);
        }

    }
}