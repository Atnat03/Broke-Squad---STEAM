using System;

namespace Gameplay.IA
{
    [Serializable]
    public class G_KoState : G_BaseState
    {
        public override void OnEnter()
        {
            _behavior.SetColor(_color);

            _motor.StopAgent();
        }
    }
}