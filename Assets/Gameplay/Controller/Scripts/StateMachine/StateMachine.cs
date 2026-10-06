namespace Gameplay.Controller
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class StateMachine
    {
        private StateNode _current;
        private readonly Dictionary<Type, StateNode> _nodes = new Dictionary<Type, StateNode>();
        private readonly List<ITransition> _anyTransitions = new List<ITransition>();
        
        public IState CurrentState => _current?.State;
        public string CurrentStateName => _current?.State?.GetType().Name ?? "None";
    
        public void Update() => _current?.State.OnUpdate();
        
        public void FixedUpdate()
        {
            ITransition transition = GetTransition();
            if (transition != null) ChangeState(transition.TargetState);
            _current?.State.OnFixedUpdate();
        }
    
        public void LateUpdate() => _current?.State.OnLateUpdate();
    
        public void SetState(IState newState)
        {
            _current = _nodes[newState.GetType()];
            _current.State?.OnEnter();
        }
        
        private void ChangeState(IState state)
        {
            if(state == _current.State) return;
            
            IState previousState = _current.State;
            IState nextState = _nodes[state.GetType()].State;
            
            previousState?.OnExit();
            nextState?.OnEnter();
            _current = _nodes[state.GetType()];
        }
    
        private ITransition GetTransition()
        {
            foreach (ITransition t in _anyTransitions)
                if (t.TargetState != _current.State && t.Condition.Evaluate())
                    return t;

            foreach (ITransition t in _current.Transitions)
                if (t.TargetState != _current.State && t.Condition.Evaluate())
                    return t;
            
            return null;
        }
    
        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition);
        }
    
        public void AddAnyTransition(IState to, IPredicate condition)
        {
            _anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
        }
        
        private StateNode GetOrAddNode(IState state)
        {
            StateNode node = _nodes.GetValueOrDefault(state.GetType());
    
            if (node == null)
            {
                node = new StateNode(state);
                _nodes.Add(state.GetType(), node);
            }
            return node;
        }
        
    
        private class StateNode
        {
            public IState State { get; }
            public HashSet<ITransition> Transitions { get; }
    
            public StateNode(IState state)
            {
                this.State = state;
                this.Transitions = new HashSet<ITransition>();
            }
    
            public void AddTransition(IState nextState, IPredicate cond)
            {
                Transitions.Add(new Transition(nextState, cond));
            }
        }
    }
}