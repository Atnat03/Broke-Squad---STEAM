using System;
using UnityEngine;

namespace Gameplay.Controller
{
    [Serializable]
    public class LadderChecker
    {
        
        [SerializeField] private LayerMask ladderLayer;
        [SerializeField] private float checkDistance = 0.35f;
        [SerializeField] private float lowRayHeight = 0.3f;
        [SerializeField] private float highRayOffset = 0.1f;
        
        [SerializeField, Range(0f, 90f)] private float fanHalfAngle = 50f;
        [SerializeField, Range(1, 9)] private int rayCount = 5;
        [SerializeField] private float castRadius = 0.12f;
        [SerializeField, Range(-1f, 1f)] private float minFacing = 0.2f;

        public bool Touching { get; private set; }
        public bool HeadTouching { get; private set; }
        public bool ClearedTop => !Touching && !HeadTouching;
        
        public Vector3 LadderNormal { get; private set; }
        public Vector3 LadderPoint { get; private set; }

        public void Check(PlayerBody body, Vector3 probeDir)
        {
            probeDir.y = 0f;
            if (probeDir.sqrMagnitude < 0.001f)
            {
                Touching = HeadTouching = false;
                return;
            }
            probeDir.Normalize();

            float dist = body.WorldRadius + checkDistance;
            Vector3 feet = body.FeetPosition;
            Vector3 low = feet + Vector3.up * lowRayHeight;
            Vector3 high = feet + Vector3.up * (body.TargetHeight * body.ScaleY - highRayOffset);

            Touching = Fan(low, probeDir, dist, out RaycastHit lowHit);
            HeadTouching = Fan(high, probeDir, dist, out RaycastHit highHit);

            if (Touching) { LadderNormal = lowHit.normal; LadderPoint = lowHit.point; }
            else if (HeadTouching) { LadderNormal = highHit.normal; LadderPoint = highHit.point; }
        }
        
        private bool Fan(Vector3 origin, Vector3 fwd, float dist, out RaycastHit best)
        {
            best = default;
            bool found = false;
            float bestDist = float.MaxValue;

            for (int i = 0; i < rayCount; i++)
            {
                float t = rayCount == 1 ? 0f : (i / (rayCount - 1f)) * 2f - 1f;
                Vector3 dir = Quaternion.AngleAxis(t * fanHalfAngle, Vector3.up) * fwd;

                bool hitSomething = Physics.SphereCast(origin, castRadius, dir, out RaycastHit hit,
                    dist, ladderLayer, QueryTriggerInteraction.Ignore);

                bool valid = hitSomething && Vector3.Dot(hit.normal, -fwd) >= minFacing;

                //Debug.DrawRay(origin, dir * dist, valid ? Color.green : Color.red);

                if (valid && hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = hit;
                    found = true;
                }
            }
            return found;
        }
        
        public float GetClimbInput(Vector3 desiredWorldDir, float deadzone = 0.3f)
        {
            Vector3 intoLadder = -LadderNormal;
            intoLadder.y = 0f;
            intoLadder.Normalize();

            float toward = Vector3.Dot(desiredWorldDir, intoLadder); 
            if (Mathf.Abs(toward) < deadzone) return 0f;
            return Mathf.Sign(toward);
        }
    }
}