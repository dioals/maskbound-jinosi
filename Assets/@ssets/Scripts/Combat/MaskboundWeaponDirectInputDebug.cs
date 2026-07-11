using MoreMountains.CorgiEngine;
using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaskboundJinosi.Combat
{
	[AddComponentMenu("Maskbound/Combat/Weapon Direct Input Debug")]
	public class MaskboundWeaponDirectInputDebug : MonoBehaviour
	{
		public bool EnabledForPrototype = true;
		public bool UseKeyboardE = true;
		public bool UseMouseLeftButton = true;
		public bool ForceDamageAreaCollider = true;
		public float ForcedColliderDuration = 2f;
		public bool DebugLogs = true;

		protected Weapon _weapon;
		protected Coroutine _forceColliderCoroutine;

		protected virtual void Awake()
		{
			_weapon = GetComponent<Weapon>();
		}

		protected virtual void Update()
		{
			if (!EnabledForPrototype || _weapon == null)
			{
				return;
			}

			if (AttackPressedThisFrame())
			{
				if (DebugLogs)
				{
					Debug.Log($"Direct weapon input start: {_weapon.name}, state: {_weapon.WeaponState.CurrentState}", this);
				}

				_weapon.WeaponInputStart();
				if (ForceDamageAreaCollider)
				{
					if (_forceColliderCoroutine != null)
					{
						StopCoroutine(_forceColliderCoroutine);
					}
					_forceColliderCoroutine = StartCoroutine(ForceDamageAreaColliderCo());
				}
			}

			if (AttackReleasedThisFrame())
			{
				if (DebugLogs)
				{
					Debug.Log($"Direct weapon input release: {_weapon.name}, state: {_weapon.WeaponState.CurrentState}", this);
				}

				_weapon.WeaponInputReleased();
			}
		}

		protected virtual IEnumerator ForceDamageAreaColliderCo()
		{
			Collider2D damageAreaCollider = FindDamageAreaCollider();
			if (damageAreaCollider == null)
			{
				if (DebugLogs)
				{
					Debug.LogWarning($"No DamageArea collider found under {_weapon.name}", this);
				}
				yield break;
			}

			damageAreaCollider.enabled = true;
			if (DebugLogs)
			{
				Debug.Log($"Forced DamageArea collider ON: {damageAreaCollider.name}", damageAreaCollider);
			}

			yield return new WaitForSeconds(Mathf.Max(0.01f, ForcedColliderDuration));

			if (damageAreaCollider != null)
			{
				damageAreaCollider.enabled = false;
				if (DebugLogs)
				{
					Debug.Log($"Forced DamageArea collider OFF: {damageAreaCollider.name}", damageAreaCollider);
				}
			}
		}

		protected virtual Collider2D FindDamageAreaCollider()
		{
			Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
			for (int i = 0; i < colliders.Length; i++)
			{
				if (colliders[i].name.Contains("DamageArea"))
				{
					return colliders[i];
				}
			}

			return colliders.Length > 0 ? colliders[0] : null;
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
