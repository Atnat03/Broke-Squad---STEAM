using System;
using Gameplay.LD;
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

        public Ladder Current { get; private set; }
        public bool Touching { get; private set; }      
        public bool HeadTouching { get; private set; }
        public bool ClearedTop => !Touching && !HeadTouching;

        public void Check(PlayerBody body, Transform root)
        {
            float dist = body.WorldRadius + checkDistance;
            Vector3 feet = body.FeetPosition;
            Vector3 fwd = root.forward;

            Vector3 low = feet + Vector3.up * lowRayHeight;
            Vector3 high = feet + Vector3.up * (body.TargetHeight * body.ScaleY - highRayOffset);

            Touching = Physics.Raycast(low, fwd, out RaycastHit lowHit, dist, ladderLayer, QueryTriggerInteraction.Ignore);
            HeadTouching = Physics.Raycast(high, fwd, out RaycastHit highHit, dist, ladderLayer, QueryTriggerInteraction.Ignore);

            if (Touching) Current = lowHit.collider.GetComponentInParent<Ladder>();
            else if (HeadTouching) Current = highHit.collider.GetComponentInParent<Ladder>();
            else Current = null;

            Debug.DrawRay(low, fwd * dist, Touching ? Color.green : Color.red);
            Debug.DrawRay(high, fwd * dist, HeadTouching ? Color.green : Color.red);
        }
    }
}