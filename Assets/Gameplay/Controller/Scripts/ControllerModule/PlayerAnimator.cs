using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Controller
{
    [Serializable]
    public class PlayerAnimator : NetworkBehaviour
    {
        [SerializeField] private Animator animator;
        private bool _crouched, _moving, _sprinting, _downed, _grounded;
        
        public void SetAnimBool(bool value, string nameValue) => SetAnimBoolServerRpc(value, nameValue);
        public void SetAnimTrigger(string nameVisual) => SetAnimTriggerServerRpc(nameVisual);
        
        [Rpc(SendTo.Server)]
        private void SetAnimBoolServerRpc(bool value, string nameVisual) => ReplicateBoolRpc(value, nameVisual);
        [Rpc(SendTo.Server)]
        private void SetAnimTriggerServerRpc(string nameVisual) => ReplicateTriggerRpc(nameVisual);
       
        
        [Rpc(SendTo.Everyone)]
        void ReplicateBoolRpc(bool value, string nameVisual) => animator.SetBool(nameVisual, value);
        [Rpc(SendTo.Everyone)]
        void ReplicateTriggerRpc(string nameVisual) => animator.SetTrigger(nameVisual);


        public void UpdateState(bool crouched, bool moving, bool sprinting, bool downed, bool grounded)
        {
            Debug.Log($"[Crouched] {crouched} [Moving] {moving} [Sprinting] {sprinting} [Downed] {downed} [Grounded] {grounded}");
            if(_crouched != crouched)
            {
                SetAnimBool(crouched, "Crouched");
                _crouched = crouched;
            }
            if (_moving != moving)
            {
                SetAnimBool(moving, "Moving");
                _moving = moving;
            }
            if(_sprinting != sprinting)
            {
                SetAnimBool(sprinting, "Sprinting");
                _sprinting = sprinting;
            }
            if (_downed != downed)
            {
                SetAnimBool(downed, "Downed");
                _downed = downed;
            }
            if (_grounded != grounded)
            {
                SetAnimBool(grounded, "Grounded");
                _grounded = grounded;
            }
        }
        
        
        
    }
}