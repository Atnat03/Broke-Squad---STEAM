using UnityEngine;

namespace Gameplay.Controller
{
    public class StepClimber
    {
        private readonly Rigidbody _rb;
        private readonly PlayerBody _body;
        private readonly GroundChecker _ground;
        private readonly ControllerProfileSO _p;

        public StepClimber(Rigidbody rb, PlayerBody body, GroundChecker ground, ControllerProfileSO profile)
        {
            _rb = rb;
            _body = body; 
            _ground = ground;
            _p = profile;
        }

        public void Tick(Vector3 desiredDir)
        {
            desiredDir.y = 0f;
            if (!_ground.IsGrounded || desiredDir.sqrMagnitude < 0.01f) return;
            Vector3 dir = desiredDir.normalized;

            LayerMask mask = _ground.GroundLayer;
            float scale = _body.ScaleY;
            float maxH = _p.maxStepHeight * scale;
            float minH = _p.minStepHeight * scale;
            Vector3 feet = _body.FeetPosition;
            float reach = _body.WorldRadius + _p.stepProbeDistance;

            Vector3 lowOrigin = feet + Vector3.up * minH;
            if (!Physics.Raycast(lowOrigin, dir, out RaycastHit lowHit, reach, mask, QueryTriggerInteraction.Ignore)) return;
            if (Vector3.Angle(lowHit.normal, Vector3.up) <= _ground.MaxSlopeAngle) return;

            Vector3 highOrigin = feet + Vector3.up * (maxH + 0.05f);
            if (Physics.Raycast(highOrigin, dir, reach + 0.1f, mask, QueryTriggerInteraction.Ignore)) return;

            Vector3 downOrigin = lowHit.point + dir * 0.05f;
            downOrigin.y = feet.y + maxH + 0.05f;
            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit topHit, maxH + 0.05f, mask, QueryTriggerInteraction.Ignore)) return;
            if (Vector3.Angle(topHit.normal, Vector3.up) > _ground.MaxSlopeAngle) return;

            float h = topHit.point.y - feet.y;
            if (h < minH || h > maxH) return;

            float r = _body.WorldRadius * 0.95f;
            Vector3 shift = Vector3.up * h + dir * _p.stepForwardNudge;
            Vector3 bottom = feet + shift + Vector3.up * (r + 0.05f);
            Vector3 top = feet + shift + Vector3.up * (_body.TargetHeight * scale - r);
            if (Physics.CheckCapsule(bottom, top, r, mask, QueryTriggerInteraction.Ignore)) return;

            _rb.position += Vector3.up * (h + 0.005f) + dir * _p.stepForwardNudge;
            Vector3 v = _rb.linearVelocity;
            if (v.y < 0f) { v.y = 0f; _rb.linearVelocity = v; }
        }
    }
}