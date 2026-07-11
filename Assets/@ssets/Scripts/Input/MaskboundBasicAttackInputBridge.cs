using MoreMountains.CorgiEngine;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaskboundJinosi.Input
{
	[AddComponentMenu("Maskbound/Input/Basic Attack Input Bridge")]
	public class MaskboundBasicAttackInputBridge : MonoBehaviour
	{
		public bool UseKeyboardE = true;
		public bool UseMouseLeftButton = true;
		public bool DirectWeaponFallback = true;
		public bool DebugLogs = true;

		protected CharacterHandleWeapon _handleWeapon;

		protected virtual void Awake()
		{
			_handleWeapon = GetComponentInParent<Character>()?.FindAbility<CharacterHandleWeapon>();
			if (_handleWeapon == null)
			{
				_handleWeapon = GetComponentInParent<CharacterHandleWeapon>();
			}
		}

		protected virtual void Update()
		{
			if (_handleWeapon == null)
			{
				return;
			}

			if (AttackPressedThisFrame())
			{
				if (DebugLogs)
				{
					Debug.Log($"Maskbound basic attack input pressed. HandleWeapon: {_handleWeapon != null}, CurrentWeapon: {_handleWeapon.CurrentWeapon}", this);
				}

				_handleWeapon.ShootStart();
				if (DirectWeaponFallback && _handleWeapon.CurrentWeapon != null)
				{
					_handleWeapon.CurrentWeapon.WeaponInputStart();
				}
			}

			if (AttackReleasedThisFrame())
			{
				_handleWeapon.ShootStop();
				if (_handleWeapon.CurrentWeapon != null)
				{
					_handleWeapon.CurrentWeapon.WeaponInputReleased();
				}
			}
		}

		protected virtual bool AttackPressedThisFrame()
		{
#if ENABLE_INPUT_SYSTEM
			if (UseKeyboardE && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
			{
				return true;
			}
			if (UseMouseLeftButton && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
			{
				return true;
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			if (UseKeyboardE && UnityEngine.Input.GetKeyDown(KeyCode.E))
			{
				return true;
			}
			if (UseMouseLeftButton && UnityEngine.Input.GetMouseButtonDown(0))
			{
				return true;
			}
#endif
			return false;
		}

		protected virtual bool AttackReleasedThisFrame()
		{
#if ENABLE_INPUT_SYSTEM
			if (UseKeyboardE && Keyboard.current != null && Keyboard.current.eKey.wasReleasedThisFrame)
			{
				return true;
			}
			if (UseMouseLeftButton && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
			{
				return true;
			}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
			if (UseKeyboardE && UnityEngine.Input.GetKeyUp(KeyCode.E))
			{
				return true;
			}
			if (UseMouseLeftButton && UnityEngine.Input.GetMouseButtonUp(0))
			{
				return true;
			}
#endif
			return false;
		}
	}
}
