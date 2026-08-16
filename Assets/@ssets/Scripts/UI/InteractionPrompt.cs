using InControl;
using MaskboundJinosi.Input;
using TMPro;
using UnityEngine;

namespace MaskboundJinosi.UI
{
    /// <summary>
    /// Sets a TMP label to the current Interact binding, adapting to the active input
    /// device (keyboard/mouse or gamepad) and refreshing automatically when it changes.
    /// </summary>
    [AddComponentMenu("Maskbound/UI/Interaction Prompt")]
    public class InteractionPrompt : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The text that shows the interact button.")]
        [SerializeField] private TMP_Text label;
        [Tooltip("Optional bindings asset. Falls back to the default bindings when empty.")]
        [SerializeField] private MaskboundInputBindings bindings;

        [Header("Format")]
        [SerializeField] private string keyboardPrefix = "Press ";
        [SerializeField] private string controllerPrefix = "Press ";
        [SerializeField] private string emptyBindingText = "-";

        private MaskboundInputBindings _runtimeFallbackBindings;

        protected virtual void Reset()
        {
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }
        }

        protected virtual void Awake()
        {
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }
        }

        protected virtual void OnEnable()
        {
            InputManager.OnActiveDeviceChanged += OnActiveDeviceChanged;
            UpdateText();
        }

        protected virtual void OnDisable()
        {
            InputManager.OnActiveDeviceChanged -= OnActiveDeviceChanged;
        }

        private void OnActiveDeviceChanged(InputDevice device)
        {
            UpdateText();
        }

        [ContextMenu("Update Text")]
        public virtual void UpdateText()
        {
            if (label == null)
            {
                return;
            }

            MaskboundInputBindings.ButtonBinding interact = GetBindings().Interact;
            label.text = ResolveButtonText(interact);
        }

        private string ResolveButtonText(MaskboundInputBindings.ButtonBinding interact)
        {
            if (interact == null)
            {
                return emptyBindingText;
            }

            InputDevice device = InputManager.ActiveDevice;
            bool keyboardInput = device == null
                || device.DeviceClass == InputDeviceClass.Keyboard
                || device.DeviceClass == InputDeviceClass.Mouse;

            if (keyboardInput)
            {
                string key = FormatKeyboard(interact);
                return key == emptyBindingText ? emptyBindingText : keyboardPrefix + key;
            }

            string button = FormatController(interact);
            return button == emptyBindingText ? emptyBindingText : controllerPrefix + button;
        }

        private string FormatKeyboard(MaskboundInputBindings.ButtonBinding binding)
        {
            if (binding.KeyboardKeys != null && binding.KeyboardKeys.Length > 0)
            {
                return FormatKey(binding.KeyboardKeys[0]);
            }

            if (binding.MouseButtons != null && binding.MouseButtons.Length > 0)
            {
                return binding.MouseButtons[0].ToString();
            }

            return emptyBindingText;
        }

        private string FormatController(MaskboundInputBindings.ButtonBinding binding)
        {
            if (binding.ControllerButtons != null && binding.ControllerButtons.Length > 0)
            {
                return FormatControlType(binding.ControllerButtons[0]);
            }

            return emptyBindingText;
        }

        private string FormatKey(Key key)
        {
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    return "Shift";
                case Key.LeftControl:
                case Key.RightControl:
                    return "Ctrl";
                case Key.LeftAlt:
                case Key.RightAlt:
                    return "Alt";
                case Key.Space:
                    return "Space";
                case Key.Return:
                    return "Enter";
                case Key.Escape:
                    return "Esc";
                case Key.Backspace:
                    return "Backspace";
                default:
                    return key.ToString();
            }
        }

        private string FormatControlType(InputControlType controlType)
        {
            switch (controlType)
            {
                case InputControlType.DPadUp:
                    return "\u25B2";
                case InputControlType.DPadDown:
                    return "\u25BC";
                case InputControlType.DPadLeft:
                    return "\u25C0";
                case InputControlType.DPadRight:
                    return "\u25B6";
                case InputControlType.Action1:
                    return "A";
                case InputControlType.Action2:
                    return "B";
                case InputControlType.Action3:
                    return "X";
                case InputControlType.Action4:
                    return "Y";
                case InputControlType.LeftBumper:
                    return "LB";
                case InputControlType.RightBumper:
                    return "RB";
                case InputControlType.LeftTrigger:
                    return "LT";
                case InputControlType.RightTrigger:
                    return "RT";
                case InputControlType.Start:
                    return "Start";
                case InputControlType.Select:
                    return "Select";
                case InputControlType.Back:
                    return "Back";
                default:
                    return controlType.ToString();
            }
        }

        private MaskboundInputBindings GetBindings()
        {
            if (bindings != null)
            {
                return bindings;
            }

            if (_runtimeFallbackBindings == null)
            {
                _runtimeFallbackBindings = ScriptableObject.CreateInstance<MaskboundInputBindings>();
            }

            return _runtimeFallbackBindings;
        }
    }
}
