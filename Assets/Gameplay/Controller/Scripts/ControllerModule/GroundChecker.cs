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
        public LayerMask GroundLayer => groundLayer;
        public float MaxSlopeAngle => maxSlopeAngle;
        public bool IsGrounded { get; private set; }
        public Vector3 Normal { get; private set; } = Vector3.up;
        public float Gap { get; private set; }

        public void ForceAirborne() => IsGrounded = false;

        public void Check(PlayerBody body, Rigidbody rb, Transform root)
        {
            float radius = body.WorldRadius * radiusMultiplier;
            Vector3 origin = body.FeetPosition + Vector3.up * (radius + skin);
            float dist = skin + checkDist;

            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, _hits, dist,
                groundLayer, QueryTriggerInteraction.Ignore);

            bool hitGround = false;
            float best = float.MaxValue;
            Vector3 bestNormal = Vector3.up;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(root)) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle) continue;

                if (hit.distance < best)
                {
                    best = hit.distance;
                    bestNormal = hit.normal;
                    hitGround = true;
                }
            }

            Normal = hitGround ? bestNormal : Vector3.up;
            IsGrounded = hitGround && Vector3.Dot(rb.linearVelocity, Normal) < 0.1f;
            
            Gap = hitGround ? Mathf.Max(0f, best - skin) : 0f;

            Debug.DrawRay(origin, Vector3.down * dist, IsGrounded ? Color.green : Color.red);
        }
    }
}