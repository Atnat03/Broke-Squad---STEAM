using System;
using UnityEngine;

namespace Gameplay.LD
{
    public class BreakableEffect : LDEffectModule
    {
        [SerializeField] private GameObject _normalMesh;
        [SerializeField] private Rigidbody[] _breakMesh;

        [Tooltip("Intensité de l'explosion des morceaux")]
        [SerializeField] private float _explosionForce = 300f;

        [Tooltip("Le flag qui indique que l'objet est cassé")]
        [SerializeField] private LDFlag _brokenFlag = LDFlag.Triggered;

        public override EffectTarget RunsOn => EffectTarget.AllClients;

        protected override void OnInitialize() => SetBroken(Context.HasFlag(_brokenFlag));

        public override void Activate(LDActivation activation)
        {
            SetBroken(true);
            float radius = Context.Element.transform.localScale.magnitude;
            foreach (var rb in _breakMesh)
                rb.AddExplosionForce(_explosionForce, transform.position, radius);
        }

        private void SetBroken(bool broken)
        {
            if (_normalMesh != null) _normalMesh.SetActive(!broken);
            foreach (var rb in _breakMesh) rb.gameObject.SetActive(broken);
        }
    }
}
