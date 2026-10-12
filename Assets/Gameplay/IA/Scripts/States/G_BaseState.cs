using System;
using Gameplay.Controller;
using Gameplay.IA.Scripts;
using UnityEngine;

namespace Gameplay.IA
{
    [Serializable]
    public abstract class G_BaseState : IState
    {
        protected GuardBehavior _behavior;
        protected GuardMotor _motor;
        protected GuardHealth _health;
        protected GuardLineOfSight _lineOfSight;
        protected Transform _myTransform;

        [SerializeField] protected Color _color;
        
        public void SetBehaviour(GuardBehavior behavior)
        { 
            _behavior = behavior;

            if (!_behavior) return;
            
            _motor = _behavior.GuardMotor;
            _health = _behavior.GuardHealth;
            _lineOfSight = _behavior.GuardSight;
            _myTransform = _behavior.transform;
        }

        public virtual void OnEnter() {}
        public virtual void OnExit() {}
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
        public virtual void OnLateUpdate() {}
    }
}