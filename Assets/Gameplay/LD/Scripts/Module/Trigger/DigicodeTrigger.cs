using Gameplay.Items;
using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.LD
{
	public enum DigicodeDisplayState : byte { Normal, Wrong, Success }

    public class DigicodeTrigger : LDTriggerModule
    {
        public const int ClearKey = 10;
        public const int ConfirmKey = 11;

        [Header("Code : ")]
        [SerializeField] private string _code = "1234";
        [SerializeField] private float _wrongCooldown = 2f;
        [SerializeField] private float _range = 4f;
        [Tooltip("Flag posé par le binding quand le code est bon")]
        [SerializeField] private LDFlag _unlockedFlag = LDFlag.Activated;

		[Header("KeyPad View")]
		[SerializeField] private TextMeshProUGUI _display;
		[SerializeField] Color _normalColor = Color.white;
		[SerializeField] Color _wrongColor = Color.red;
		[SerializeField] Color _successColor = Color.green;

		private readonly NetworkVariable<FixedString32Bytes> _buffer = new(default);
		private readonly NetworkVariable<DigicodeDisplayState> _displayState = new(DigicodeDisplayState.Normal);
		private bool _wasUnlocked;

		protected override void OnInitialize()
		{
			_buffer.OnValueChanged += OnBufferChanged;
			_displayState.OnValueChanged += OnDisplayStateChanged;
			RefreshDisplay();

			if (!Context.IsServer) return;
			_wasUnlocked = Context.HasFlag(_unlockedFlag);
			Context.OnFlagChanged += OnFlagsChanged;
		}

		public void ServerPress(NetworkObject player, int key)
		{
			if (!Context.IsServer) return;
			if (Context.HasFlag(_unlockedFlag)) return;
			if (_displayState.Value != DigicodeDisplayState.Normal) return;
			if (Vector3.Distance(player.transform.position, transform.position) > _range) return;

			if(key == ClearKey)
			{
				_buffer.Value = default;
				return;
			}

			if (key == ConfirmKey)
			{
				Confirm(player);
				return;
			}

			if (key < 0 || key > 9) return;
			if (_buffer.Value.Length >= _code.Length) return;
			_buffer.Value = new FixedString32Bytes(_buffer.Value.ToString() + key);

		}

		private void Confirm(NetworkObject player)
		{
			if (_buffer.Value.Length == 0) return;

			if (_buffer.Value.ToString() == _code)
			{
				_displayState.Value = DigicodeDisplayState.Success;
				Raise(new LDActivation(player, player.transform.position));
			}
			else
			{
				_displayState.Value = DigicodeDisplayState.Wrong;
				StartCoroutine(ResetAfterWrong());
			}
		}

		private IEnumerator ResetAfterWrong()
		{
			yield return new WaitForSeconds(_wrongCooldown);
			_buffer.Value = default;
			_displayState.Value = DigicodeDisplayState.Normal;
		}

		private void OnFlagsChanged(LDFlag flag)
		{
			bool unlocked = (flag & _unlockedFlag) != 0;
			if(_wasUnlocked && !unlocked)
			{
				_buffer.Value = default;
				_displayState.Value = DigicodeDisplayState.Normal;
			}
			_wasUnlocked = unlocked;
		}

		private void RefreshDisplay()
		{
			if (_display == null) return;
			_display.text = _buffer.Value.ToString();
			Debug.Log($"[LD] buffer : {_display.text}");
			_display.color = _displayState.Value switch
			{
				DigicodeDisplayState.Wrong => _wrongColor,
				DigicodeDisplayState.Success => _successColor,
				_ => _normalColor
			};
		}

		private void OnDisplayStateChanged(DigicodeDisplayState previousValue, DigicodeDisplayState newValue) => RefreshDisplay();

		private void OnBufferChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue) => RefreshDisplay();
	}
}
