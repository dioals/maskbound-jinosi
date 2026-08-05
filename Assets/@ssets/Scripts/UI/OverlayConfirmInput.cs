using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaskboundJinosi.UI
{
    public static class OverlayConfirmInput
    {
        public static bool WasPressedThisFrame()
        {
            if (InputManager.HasInstance && InputManager.Instance.ShootButton != null &&
                InputManager.Instance.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Gamepad.current != null &&
                (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                 Gamepad.current.buttonWest.wasPressedThisFrame ||
                 Gamepad.current.rightShoulder.wasPressedThisFrame ||
                 Gamepad.current.rightTrigger.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(KeyCode.E) ||
                   UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0) ||
                   UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton2) ||
                   UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton5);
#else
            return false;
#endif
        }
    }
}
