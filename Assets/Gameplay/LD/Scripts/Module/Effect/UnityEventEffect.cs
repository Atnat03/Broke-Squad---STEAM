using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.LD
{

	public class UnityEventEffect : LDEffectModule
	{
		[SerializeField] private EffectTarget _runsOn;
		[SerializeField] private UnityEvent _OnActrivate;

		public override EffectTarget RunsOn => _runsOn;

		public override void Activate(LDActivation activation) => _OnActrivate.Invoke();
	}
}