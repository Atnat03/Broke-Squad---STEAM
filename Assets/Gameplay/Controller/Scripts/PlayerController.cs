using UnityEngine;

namespace Gameplay.Controller
{
    public class PlayerController : MonoBehaviour
    {
        #region variables

        private StateMachine _stateMachine;

        #endregion
        
        #region Initialization
        
        
        void Awake()
        {
            SetUpStateMachine();
        }

        private void SetUpStateMachine()
        {
            _stateMachine = new StateMachine();
            
            
        }
        
        #endregion
        
    }
}