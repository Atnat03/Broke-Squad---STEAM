using System;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay.IA
{
    [Serializable]
    public class G_PatrolState : G_BaseState
    {
        [SerializeField] Transform _target;
        
        public override void OnEnter()
        {
            _behavior.Agent.SetDestination(_target.position);
        }
    }
}