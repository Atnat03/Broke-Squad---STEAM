using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD
{
    public class EventEffect : LDEffectModule
    {
        [SerializeField] private UnityEvent _onActivate;

        public override EffectTarget RunsOn => EffectTarget.AllClients;

        public override void Activate(LDActivation activation) => _onActivate.Invoke();

    }
}
