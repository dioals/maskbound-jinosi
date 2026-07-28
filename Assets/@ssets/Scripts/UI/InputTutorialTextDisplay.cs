using System;
using System.Text;
using InControl;
using MaskboundJinosi.Input;
using TMPro;
using UnityEngine;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Input Tutorial Text Display")]
	public class InputTutorialTextDisplay : MonoBehaviour
	{
		public enum DisplayMode
		{
			KeyboardAndController,
			KeyboardOnly,
			ControllerOnly
		}

		[Header("References")]
		public TMP_Text TargetText;
		public MaskboundInputBindings InputBindings;

		[Header("Content")]
		public DisplayMode Mode = DisplayMode.KeyboardAndController;
		public bool ShowMovement = true;
		public bool ShowCombat = true;
		public bool ShowUtility = true;
		public bool ShowAdvanced = false;
		public string Title = "Controls";

		[Header("Format")]
		public string EmptyBindingText = "-";
		public string KeyboardLabel = "Keyboard";
		public string ControllerLabel = "Controller";
		public string SectionSeparator = "\n";

		private readonly StringBuilder _builder = new StringBuilder(512);
		private MaskboundInputBindings _runtimeFallbackBindings;

		protected virtual void Reset()
		{
			TargetText = GetComponent<TMP_Text>();
		}

		protected virtual void Awake()
		{
			if (TargetText == null)
			{
				TargetText = GetComponent<TMP_Text>();
			}
		}

		protected virtual void OnEnable()
		{
			Refresh();
		}

		protected virtual void OnValidate()
		{
			if (!isActiveAndEnabled)
			{
				return;
			}

			Refresh();
		}

		[ContextMenu("Refresh Tutorial Text")]
		public virtual void Refresh()
		{
			if (TargetText == null)
			{
				return;
			}

			MaskboundInputBindings bindings = GetBindings();
			_builder.Length = 0;

			if (!string.IsNullOrWhiteSpace(Title))
			{
				_builder.AppendLine(Title);
			}

			if (ShowMovement)
			{
				AppendSection("Movement");
				AppendAxis(bindings.Horizontal);
				AppendAxis(bindings.Vertical);
			}

			if (ShowCombat)
			{
				AppendSection("Combat");
				AppendButton(bindings.Attack);
				AppendButton(bindings.SpecialAttack);
				AppendButton(bindings.Block);
			}

			if (ShowUtility)
			{
				AppendSection("Utility");
				AppendButton(bindings.Jump);
				AppendButton(bindings.Run);
				AppendButton(bindings.Dash);
				AppendButton(bindings.Interact);
				AppendButton(bindings.Pause);
			}

			if (ShowAdvanced)
			{
				AppendSection("Advanced");
				AppendButton(bindings.Roll);
				AppendButton(bindings.Reload);
				AppendButton(bindings.SwitchWeapon);
				AppendButton(bindings.SwitchCharacter);
				AppendButton(bindings.TimeControl);
				AppendButton(bindings.Grab);
				AppendButton(bindings.Throw);
				AppendButton(bindings.Push);
				AppendButton(bindings.Grip);
				AppendButton(bindings.Swim);
				AppendButton(bindings.Glide);
				AppendButton(bindings.Jetpack);
				AppendButton(bindings.Fly);
				AppendAxis(bindings.AimHorizontal);
				AppendAxis(bindings.AimVertical);
			}

			TargetText.text = _builder.ToString().TrimEnd();
		}

		private MaskboundInputBindings GetBindings()
		{
			if (InputBindings != null)
			{
				return InputBindings;
			}

			if (_runtimeFallbackBindings == null)
			{
				_runtimeFallbackBindings = ScriptableObject.CreateInstance<MaskboundInputBindings>();
			}

			return _runtimeFallbackBindings;
		}

		private void AppendSection(string sectionName)
		{
			if (_builder.Length > 0)
			{
				_builder.Append(SectionSeparator);
			}

			_builder.AppendLine(sectionName);
		}

		private void AppendAxis(MaskboundInputBindings.AxisBinding axis)
		{
			if (axis == null)
			{
				return;
			}

			AppendButton(axis.Negative);
			AppendButton(axis.Positive);
		}

		private void AppendButton(MaskboundInputBindings.ButtonBinding binding)
		{
			if (binding == null)
			{
				return;
			}

			string label = string.IsNullOrWhiteSpace(binding.Label) ? "Input" : binding.Label;
			_builder.Append(label);
			_builder.Append(": ");

			switch (Mode)
			{
				case DisplayMode.KeyboardOnly:
					_builder.Append(FormatKeyboard(binding));
					break;
				case DisplayMode.ControllerOnly:
					_builder.Append(FormatController(binding));
					break;
				default:
					_builder.Append(KeyboardLabel);
					_builder.Append(" ");
					_builder.Append(FormatKeyboard(binding));
					_builder.Append(" / ");
					_builder.Append(ControllerLabel);
					_builder.Append(" ");
					_builder.Append(FormatController(binding));
					break;
			}

			_builder.AppendLine();
		}

		private string FormatKeyboard(MaskboundInputBindings.ButtonBinding binding)
		{
			string keyboard = FormatValues(binding.KeyboardKeys);
			string mouse = FormatValues(binding.MouseButtons);

			if (keyboard == EmptyBindingText)
			{
				return mouse;
			}

			if (mouse == EmptyBindingText)
			{
				return keyboard;
			}

			return $"{keyboard}, {mouse}";
		}

		private string FormatController(MaskboundInputBindings.ButtonBinding binding)
		{
			return FormatValues(binding.ControllerButtons);
		}

		private string FormatValues<T>(T[] values) where T : struct, IConvertible
		{
			if (values == null || values.Length == 0)
			{
				return EmptyBindingText;
			}

			StringBuilder valueBuilder = new StringBuilder();
			for (int i = 0; i < values.Length; i++)
			{
				if (i > 0)
				{
					valueBuilder.Append(", ");
				}

				valueBuilder.Append(values[i]);
			}

			return valueBuilder.ToString();
		}
	}
}
