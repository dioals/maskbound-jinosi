using InControl;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Input
{
	[AddComponentMenu("Maskbound/Input/InControl Weapon Input")]
	public class MaskboundInControlWeaponInput : MonoBehaviour
	{
		public bool EnabledForPrototype = true;
		public bool DebugLogs = true;
		public bool ForceDamageAreaCollider = true;
		public float ForcedColliderDuration = 2f;

		protected Weapon _weapon;
		protected MaskboundWeaponActions _actions;
		protected float _forcedColliderUntil;
		protected Collider2D _damageAreaCollider;

		protected virtual void Awake()
		{
			_weapon = GetComponent<Weapon>();
			_actions = MaskboundWeaponActions.CreateWithDefaultBindings();
		}

		protected virtual void OnDestroy()
		{
			_actions?.Destroy();
		}

		protected virtual void Update()
		{
			if (!EnabledForPrototype || _weapon == null || _actions == null)
			{
				return;
			}

			if (_actions.Attack.WasPressed)
			{
				if (DebugLogs)
				{
					Debug.Log($"InControl attack pressed. Weapon: {_weapon.name}, state: {_weapon.WeaponState.CurrentState}, device: {_actions.LastDeviceClass}", this);
				}

				_weapon.WeaponInputStart();
				if (ForceDamageAreaCollider)
				{
					ForceColliderOn();
				}
			}

			if (_actions.Attack.WasReleased)
			{
				if (DebugLogs)
				{
					Debug.Log($"InControl attack released. Weapon: {_weapon.name}, state: {_weapon.WeaponState.CurrentState}", this);
				}

				_weapon.WeaponInputReleased();
			}

			if (_damageAreaCollider != null && _damageAreaCollider.enabled && Time.time >= _forcedColliderUntil)
			{
				_damageAreaCollider.enabled = false;
				if (DebugLogs)
				{
					Debug.Log($"InControl forced DamageArea collider OFF: {_damageAreaCollider.name}", _damageAreaCollider);
				}
			}
		}

		protected virtual void ForceColliderOn()
		{
			_damageAreaCollider = FindDamageAreaCollider();
			if (_damageAreaCollider == null)
			{
				if (DebugLogs)
				{
					Debug.LogWarning($"InControl input received, but no DamageArea collider found under {_weapon.name}", this);
				}
				return;
			}

			_forcedColliderUntil = Time.time + Mathf.Max(0.01f, ForcedColliderDuration);
			_damageAreaCollider.enabled = true;
			if (DebugLogs)
			{
				Debug.Log($"InControl forced DamageArea collider ON: {_damageAreaCollider.name}", _damageAreaCollider);
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

		protected class MaskboundWeaponActions : PlayerActionSet
		{
			public readonly PlayerAction Attack;

			protected MaskboundWeaponActions()
			{
				Attack = CreatePlayerAction("Attack");
			}

			public static MaskboundWeaponActions CreateWithDefaultBindings()
			{
				MaskboundWeaponActions actions = new MaskboundWeaponActions();
				actions.Attack.AddDefaultBinding(Key.E);
				actions.Attack.AddDefaultBinding(Mouse.LeftButton);
				actions.Attack.AddDefaultBinding(InputControlType.Action1);
				actions.Attack.AddDefaultBinding(InputControlType.RightTrigger);
				return actions;
			}
		}
	}
}
