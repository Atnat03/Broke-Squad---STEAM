using System;
using System.Collections;
using Gameplay.LD;
using Gameplay.LD.Scripts;
using MyPrint;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Items
{
    [Serializable]
    public class MeleeDamageModule : ActionModule, IServerAction, IFirstAction
    {
        [SerializeField] private float _range = 3f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _attackCooldown = 1f;

        private bool _isAttacking = false;
        private Coroutine _attackCoroutine;
        
        public ICondition Condition => ConditionParent;

        public void StartFirstAction()
        {
            if (!CanUse()) return;
            
            if (_isAttacking) return;
            
            _isAttacking = true;
            
            _attackCoroutine = Context.StartCoroutine(PerformAttackRoutine());
            
            Transform cam = Context.Camera.transform;

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2f))
            {
                if (hit.transform.TryGetComponent(out NetworkObject n))
                {
                    Context.SendToServer(this, new OnModuleDoAction_EVENT
                    {
                        Target = n
                    });
                    
                    ConsumeCondition(new OnModuleDoAction_EVENT());
                }
            }
        }

        IEnumerator PerformAttackRoutine()
        {
            yield return new WaitForSeconds(_attackCooldown);
            
            _isAttacking = false;
        }
        
        public void StopFirstAction() { }
                
        public void ServerExecute(OnModuleDoAction_EVENT data)
        {
            NetworkObject n = data.GetTarget();

            if (n.TryGetComponent(out IDamageable damageable))
            {
                damageable.ApplyDamage(_damage);
            }
        }
        
        public void OnNotInHand()
        {
            ResetAttack();
        }

        public void OnThrow()
        {
            ResetAttack();
        }

        void ResetAttack()
        {
            Context.StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
            _isAttacking = false;
        }
    }
}