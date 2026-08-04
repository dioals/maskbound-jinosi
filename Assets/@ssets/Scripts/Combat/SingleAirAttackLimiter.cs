using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
	[AddComponentMenu("Maskbound/Combat/Single Air Attack Limiter")]
	public class SingleAirAttackLimiter : MonoBehaviour
	{
		public bool LimitAirAttacks = true;
		[Min(1)] public int MaximumAirAttacks = 2;

		protected Character _character;
		protected CorgiController _controller;
		protected CharacterHandleWeapon _handleWeapon;
		protected int _airAttacksUsed;
		protected bool _abilityWasPermitted = true;
		protected bool _abilityLockedByLimiter;
		protected bool _weaponWasActive;
		protected bool _registeredByDirectInput;

		public bool CanAttack => !LimitAirAttacks || IsGrounded || _airAttacksUsed < MaximumAirAttacks;
		public bool IsGrounded => _controller != null && _controller.State.IsGrounded;

		protected virtual void Awake()
		{
			ResolveReferences();
		}

		protected virtual void Update()
		{
			ResolveReferences();
			if (_controller == null || _handleWeapon == null)
			{
				return;
			}

			if (IsGrounded)
			{
				ResetAirAttack();
				return;
			}

			bool weaponIsActive = _handleWeapon.CurrentWeapon != null &&
				_handleWeapon.CurrentWeapon.WeaponState.CurrentState != Weapon.WeaponStates.WeaponIdle;

			if (weaponIsActive && !_weaponWasActive)
			{
				if (_registeredByDirectInput)
				{
					_registeredByDirectInput = false;
				}
				else if (_airAttacksUsed < MaximumAirAttacks)
				{
					RegisterAirAttack();
				}
			}

			_weaponWasActive = weaponIsActive;
		}

		public virtual bool TryConsumeAirAttack()
		{
			ResolveReferences();
			if (!LimitAirAttacks || IsGrounded)
			{
				return true;
			}

			if (_airAttacksUsed >= MaximumAirAttacks)
			{
				return false;
			}

			RegisterAirAttack();
			_registeredByDirectInput = true;
			return true;
		}

		protected virtual void RegisterAirAttack()
		{
			_airAttacksUsed++;
			if (_airAttacksUsed < MaximumAirAttacks || _handleWeapon == null || _abilityLockedByLimiter)
			{
				return;
			}

			_abilityWasPermitted = _handleWeapon.AbilityPermitted;
			_handleWeapon.PermitAbility(false);
			_abilityLockedByLimiter = true;
		}

		protected virtual void ResetAirAttack()
		{
			_airAttacksUsed = 0;
			_weaponWasActive = false;
			_registeredByDirectInput = false;
			if (_handleWeapon != null && _abilityLockedByLimiter)
			{
				_handleWeapon.PermitAbility(_abilityWasPermitted);
			}
			_abilityLockedByLimiter = false;
		}

		protected virtual void OnDisable()
		{
			ResetAirAttack();
		}

		protected virtual void ResolveReferences()
		{
			if (_character == null)
			{
				_character = GetComponentInParent<Character>();
			}
			if (_character == null)
			{
				return;
			}

			if (_controller == null)
			{
				_controller = _character.GetComponent<CorgiController>();
			}
			if (_handleWeapon == null)
			{
				_handleWeapon = _character.FindAbility<CharacterHandleWeapon>();
			}
		}
	}
}
