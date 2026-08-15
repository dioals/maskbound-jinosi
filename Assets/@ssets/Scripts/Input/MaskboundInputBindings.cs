using System;
using InControl;
using UnityEngine;

namespace MaskboundJinosi.Input
{
    [CreateAssetMenu(
        fileName = "MaskboundInputBindings",
        menuName = "Maskbound/Input/Input Bindings")]
    public class MaskboundInputBindings : ScriptableObject
    {
        [Serializable]
        public class ButtonBinding
        {
            public string Label;
            public Key[] KeyboardKeys = Array.Empty<Key>();
            public Mouse[] MouseButtons = Array.Empty<Mouse>();
            public InputControlType[] ControllerButtons = Array.Empty<InputControlType>();

            public void ApplyTo(PlayerAction action)
            {
                AddKeyboardBindings(action);
                AddMouseBindings(action);
                AddControllerBindings(action);
            }

            private void AddKeyboardBindings(PlayerAction action)
            {
                for (int i = 0; i < KeyboardKeys.Length; i++)
                {
                    action.AddDefaultBinding(KeyboardKeys[i]);
                }
            }

            private void AddMouseBindings(PlayerAction action)
            {
                for (int i = 0; i < MouseButtons.Length; i++)
                {
                    action.AddDefaultBinding(MouseButtons[i]);
                }
            }

            private void AddControllerBindings(PlayerAction action)
            {
                for (int i = 0; i < ControllerButtons.Length; i++)
                {
                    action.AddDefaultBinding(ControllerButtons[i]);
                }
            }
        }

        [Serializable]
        public class AxisBinding
        {
            public string Label;
            public ButtonBinding Negative = new ButtonBinding();
            public ButtonBinding Positive = new ButtonBinding();
        }

        [Header("Movement")]
        public AxisBinding Horizontal = new AxisBinding
        {
            Label = "Horizontal",
            Negative = new ButtonBinding
            {
                Label = "Move Left",
                KeyboardKeys = new[] { Key.A, Key.LeftArrow },
                ControllerButtons = new[] { InputControlType.LeftStickLeft, InputControlType.DPadLeft }
            },
            Positive = new ButtonBinding
            {
                Label = "Move Right",
                KeyboardKeys = new[] { Key.D, Key.RightArrow },
                ControllerButtons = new[] { InputControlType.LeftStickRight, InputControlType.DPadRight }
            }
        };

        public AxisBinding Vertical = new AxisBinding
        {
            Label = "Vertical",
            Negative = new ButtonBinding
            {
                Label = "Move Down",
                KeyboardKeys = new[] { Key.S, Key.DownArrow },
                ControllerButtons = new[] { InputControlType.LeftStickDown, InputControlType.DPadDown }
            },
            Positive = new ButtonBinding
            {
                Label = "Move Up",
                KeyboardKeys = new[] { Key.W, Key.UpArrow },
                ControllerButtons = new[] { InputControlType.LeftStickUp, InputControlType.DPadUp }
            }
        };

        [Header("Aim")]
        public AxisBinding AimHorizontal = new AxisBinding
        {
            Label = "Aim Horizontal",
            Negative = new ButtonBinding { Label = "Aim Left", ControllerButtons = new[] { InputControlType.RightStickLeft } },
            Positive = new ButtonBinding { Label = "Aim Right", ControllerButtons = new[] { InputControlType.RightStickRight } }
        };

        public AxisBinding AimVertical = new AxisBinding
        {
            Label = "Aim Vertical",
            Negative = new ButtonBinding { Label = "Aim Down", ControllerButtons = new[] { InputControlType.RightStickDown } },
            Positive = new ButtonBinding { Label = "Aim Up", ControllerButtons = new[] { InputControlType.RightStickUp } }
        };

        [Header("Actions")]
        public ButtonBinding Jump = new ButtonBinding { Label = "Jump", KeyboardKeys = new[] { Key.Space }, ControllerButtons = new[] { InputControlType.Action1 } };
        public ButtonBinding Run = new ButtonBinding { Label = "Run", KeyboardKeys = new[] { Key.LeftShift }, ControllerButtons = new[] { InputControlType.LeftBumper } };
        public ButtonBinding Dash = new ButtonBinding { Label = "Dash", KeyboardKeys = new[] { Key.LeftControl }, ControllerButtons = new[] { InputControlType.Action2 } };
        public ButtonBinding Roll = new ButtonBinding { Label = "Roll", KeyboardKeys = new[] { Key.LeftAlt }, ControllerButtons = new[] { InputControlType.Action4 } };
        public ButtonBinding Attack = new ButtonBinding { Label = "Attack", KeyboardKeys = new[] { Key.E }, ControllerButtons = new[] { InputControlType.Action3, InputControlType.RightTrigger } };
        public ButtonBinding SpecialAttack = new ButtonBinding { Label = "Special Attack", MouseButtons = new[] { Mouse.RightButton }, ControllerButtons = new[] { InputControlType.Action4 } };
        public ButtonBinding Interact = new ButtonBinding { Label = "Interact", KeyboardKeys = new[] { Key.F }, ControllerButtons = new[] { InputControlType.DPadUp } };
        public ButtonBinding Reload = new ButtonBinding { Label = "Reload", KeyboardKeys = new[] { Key.R }, ControllerButtons = new[] { InputControlType.Action4 } };
        public ButtonBinding Pause = new ButtonBinding { Label = "Pause", KeyboardKeys = new[] { Key.Escape }, ControllerButtons = new[] { InputControlType.Start, InputControlType.Menu, InputControlType.Command } };
        public ButtonBinding SwitchWeapon = new ButtonBinding { Label = "Switch Weapon", KeyboardKeys = new[] { Key.Tab }, ControllerButtons = new[] { InputControlType.RightBumper } };
        public ButtonBinding SwitchCharacter = new ButtonBinding { Label = "Switch Character", KeyboardKeys = new[] { Key.C } };
        public ButtonBinding TimeControl = new ButtonBinding { Label = "Time Control", KeyboardKeys = new[] { Key.T } };
        public ButtonBinding Swim = new ButtonBinding { Label = "Swim", KeyboardKeys = new[] { Key.Space } };
        public ButtonBinding Glide = new ButtonBinding { Label = "Glide", KeyboardKeys = new[] { Key.Space } };
        public ButtonBinding Jetpack = new ButtonBinding { Label = "Jetpack", KeyboardKeys = new[] { Key.Space } };
        public ButtonBinding Fly = new ButtonBinding { Label = "Fly", KeyboardKeys = new[] { Key.Space } };
        public ButtonBinding Grab = new ButtonBinding { Label = "Grab", KeyboardKeys = new[] { Key.G } };
        public ButtonBinding Throw = new ButtonBinding { Label = "Throw", KeyboardKeys = new[] { Key.H } };
        public ButtonBinding Push = new ButtonBinding { Label = "Push", KeyboardKeys = new[] { Key.P } };
        public ButtonBinding Grip = new ButtonBinding { Label = "Grip", KeyboardKeys = new[] { Key.G } };
        public ButtonBinding Block = new ButtonBinding { Label = "Block", KeyboardKeys = new[] { Key.B } };
    }
}
