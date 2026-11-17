using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Gameplay.Animation
{
	[Serializable]
	public class HandBind
	{
		public string nameParam;
		[Range(0, 1f)]
		public float blend = 0f;
	}

	public class SnapHandAnimation : MonoBehaviour
	{
		[SerializeField] private List<HandBind> _bindList = new();
		[SerializeField] private Animator _animator;
		[SerializeField] private HandSide _hiddenHand = HandSide.Right;

		private PlayerHands _hands;

        private void OnEnable()
        {
            Sync();

            if (_animator == null) return;

            foreach (HandBind item in _bindList)
                if (!string.IsNullOrEmpty(item.nameParam))
                    _animator.SetFloat(item.nameParam, item.blend);

            _animator.Update(0f);
        }

        private void OnTransformParentChanged()
        {
            if (isActiveAndEnabled) Sync();
        }

        private void OnDisable() => Release();

        private void Sync()
        {
            PlayerHands owner = GetComponentInParent<PlayerHands>();
            if (owner == _hands) return;

            Release();
            _hands = owner;
            if (_hands != null) _hands.Hide(_hiddenHand);
        }

        private void Release()
        {
            if (_hands == null) return;
            _hands.Show(_hiddenHand);
            _hands = null;
        }
    }
}