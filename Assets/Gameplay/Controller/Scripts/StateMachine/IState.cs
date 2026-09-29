namespace Gameplay.Controller
{
    using UnityEngine;

    public interface IState
    {
        void OnEnter();
        void OnExit();
        void OnUpdate();
        void OnFixedUpdate();
        void OnLateUpdate();
    }
}

