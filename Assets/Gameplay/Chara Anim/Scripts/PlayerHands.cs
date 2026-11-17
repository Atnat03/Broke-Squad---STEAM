using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay.Animation
{
    public enum HandSide { None, Right, Left, Both}

    public class PlayerHands : MonoBehaviour
    {
        [SerializeField] private GameObject _rightHand;
        [SerializeField] private GameObject _leftHand;

        private int _rightHides;
        private int _leftHides;

        public void Show(HandSide side)
        {
            if (side == HandSide.Right || side == HandSide.Both) _rightHides++;
            if (side == HandSide.Left || side == HandSide.Both) _leftHides++;
            Apply();
        }

        public void Hide(HandSide side)
        {
            if (side == HandSide.Right || side == HandSide.Both) _rightHides = Mathf.Max(0, _rightHides - 1);
            if (side == HandSide.Left || side == HandSide.Both) _leftHides = Mathf.Max(0, _leftHides - 1);
            Apply();
        }

        private void Apply()
        {
            if (_rightHand != null) _rightHand.SetActive(_rightHides == 0);
            if (_leftHand != null) _leftHand.SetActive(_leftHides == 0);
        }
    }
}
