using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaskboundJinosi.Debugging
{
	[RequireComponent(typeof(Toggle))]
	[AddComponentMenu("Maskbound/Debug/Mod Menu Toggle Item")]
	public class DevModMenuToggleItem : MonoBehaviour
	{
		public string Label = "Option";
		public TMP_Text LabelText;
		public UnityEvent EnabledAction = new UnityEvent();
		public UnityEvent DisabledAction = new UnityEvent();

		private Toggle _toggle;

		protected virtual void Awake()
		{
			_toggle = GetComponent<Toggle>();
			_toggle.onValueChanged.AddListener(HandleChanged);
			RefreshLabel(_toggle.isOn);
		}

		public virtual void SetValueWithoutNotify(bool value)
		{
			_toggle.SetIsOnWithoutNotify(value);
			RefreshLabel(value);
		}

		private void HandleChanged(bool value)
		{
			if (value) EnabledAction?.Invoke();
			else DisabledAction?.Invoke();
			RefreshLabel(value);
		}

		private void RefreshLabel(bool value)
		{
			if (LabelText != null)
			{
				LabelText.text = $"{Label}: {(value ? "ON" : "OFF")}";
			}
		}

		protected virtual void OnDestroy()
		{
			if (_toggle != null)
			{
				_toggle.onValueChanged.RemoveListener(HandleChanged);
			}
		}
	}
}
