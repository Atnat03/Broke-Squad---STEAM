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
		[SerializeField] private Transform rightHand;

		void OnEnable()
		{
			if (_animator == null) return;

			rightHand.GetComponent<GameObject>().SetActive(false);

			foreach (var item in _bindList)
			{
				if (!string.IsNullOrEmpty(item.nameParam))
				{
					_animator.SetFloat(item.nameParam, item.blend);
				}
			}

			_animator.Update(0f);
		}

		private void OnDisable()
		{
			rightHand.GetComponent<GameObject>().SetActive(true);
		}
	}
}