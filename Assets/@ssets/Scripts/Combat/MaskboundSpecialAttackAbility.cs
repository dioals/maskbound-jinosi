using System.Collections;
using System.Collections.Generic;
using MaskboundJinosi.Skills;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
	[AddComponentMenu("Maskbound/Character/Abilities/Special Attack")]
	public class MaskboundSpecialAttackAbility : CharacterAbility
	{
		public enum SpecialInputSources
		{
			TimeControl,
			SecondaryShoot
		}

		[Header("Special Attack")]
		public MaskboundSpecialAttackData SpecialAttackData;
		public MaskboundSpecialHitbox SpecialHitbox;
		public SpecialInputSources InputSource = SpecialInputSources.TimeControl;
		public bool PreventHorizontalMovement = true;
		public SkillCooldownFeedback CooldownFeedback;

		[Header("Runtime")]
		[MMReadOnly] public bool IsSpecialAttacking;
		[MMReadOnly] public float CooldownRemaining;
		[MMReadOnly] public float CurrentEnergy = 100f;
		public float MaxEnergy = 100f;

		protected int _specialAttackTriggerParameter;
		protected int _isSpecialAttackingParameter;
		protected int _specialIndexParameter;
		protected Coroutine _attackCoroutine;
		protected float _lastUseTime = -999f;
		protected CharacterHorizontalMovement _horizontalMovement;
		protected bool _movementWasForbidden;

		public override string HelpBoxText()
		{
			return "Handles Maskbound special attacks from Corgi input. Use SpecialAttackData for timing, damage, cooldown, hitbox, and animation settings.";
		}

		protected override void Initialization()
		{
			base.Initialization();

			CurrentEnergy = Mathf.Clamp(CurrentEnergy, 0f, MaxEnergy);
			_horizontalMovement = _character?.FindAbility<CharacterHorizontalMovement>();

			if (CooldownFeedback == null && _character != null)
			{
				CooldownFeedback = _character.GetComponentInChildren<SkillCooldownFeedback>(true);
			}

			if (SpecialHitbox != null)
			{
				SpecialHitbox.Deactivate();
			}
		}

		protected override void InitializeAnimatorParameters()
		{
			base.InitializeAnimatorParameters();

			if (SpecialAttackData == null)
			{
				return;
			}

			RegisterAnimatorParameter(SpecialAttackData.SpecialAttackTriggerParameter, AnimatorControllerParameterType.Trigger, out _specialAttackTriggerParameter);
			RegisterAnimatorParameter(SpecialAttackData.IsSpecialAttackingParameter, AnimatorControllerParameterType.Bool, out _isSpecialAttackingParameter);
			RegisterAnimatorParameter(SpecialAttackData.SpecialIndexParameter, AnimatorControllerParameterType.Int, out _specialIndexParameter);
		}

		protected override void HandleInput()
		{
			base.HandleInput();

			if (!AbilityAuthorized || _inputManager == null)
			{
				return;
			}

			if (SpecialInputDown())
			{
				TryStartSpecialAttack();
			}
		}

		public override void ProcessAbility()
		{
			base.ProcessAbility();

			if (SpecialAttackData == null)
			{
				CooldownRemaining = 0f;
				return;
			}

			CooldownRemaining = Mathf.Max(0f, SpecialAttackData.Cooldown - (Time.time - _lastUseTime));
		}

		public override void UpdateAnimator()
		{
			base.UpdateAnimator();

			if ((_animator == null) || (_character == null) || (SpecialAttackData == null))
			{
				return;
			}

			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _isSpecialAttackingParameter, IsSpecialAttacking, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorInteger(_animator, _specialIndexParameter, SpecialAttackData.SpecialIndex, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		public virtual bool TryStartSpecialAttack()
		{
			if (!CanStartSpecialAttack())
			{
				if ((SpecialAttackData != null) && !IsSpecialAttacking && (CooldownRemaining > 0f))
				{
					CooldownFeedback?.Show();
				}

				return false;
			}

			if (SpecialAttackData.RequiresEnergy)
			{
				CurrentEnergy -= SpecialAttackData.EnergyCost;
			}

			_attackCoroutine = StartCoroutine(SpecialAttackSequence());
			return true;
		}

		public virtual bool CanStartSpecialAttack()
		{
			if ((SpecialAttackData == null) || IsSpecialAttacking)
			{
				return false;
			}

			if (_condition != null && _condition.CurrentState != CharacterStates.CharacterConditions.Normal)
			{
				return false;
			}

			// Don't start a special mid-attack, mid-skill-cast or while blocking: those
			// systems also lock horizontal movement, and overlapping two MovementForbidden
			// owners (weapon + special) is what leaves the flag stuck after both finish.
			if (_character != null)
			{
				if (_character.IsCastingSkill || _character.IsBlocking)
				{
					return false;
				}

				if (IsAttacking())
				{
					return false;
				}
			}

			if (Time.time < _lastUseTime + SpecialAttackData.Cooldown)
			{
				return false;
			}

			if (SpecialAttackData.RequiresEnergy && CurrentEnergy < SpecialAttackData.EnergyCost)
			{
				return false;
			}

			return true;
		}

		protected virtual bool IsAttacking()
		{
			if (_character == null)
			{
				return false;
			}

			List<CharacterHandleWeapon> handleWeapons = _character.FindAbilities<CharacterHandleWeapon>();
			if (handleWeapons == null)
			{
				return false;
			}

			foreach (CharacterHandleWeapon handleWeapon in handleWeapons)
			{
				if ((handleWeapon.CurrentWeapon != null)
				    && (handleWeapon.CurrentWeapon.WeaponState.CurrentState != Weapon.WeaponStates.WeaponIdle))
				{
					return true;
				}
			}

			return false;
		}

		public virtual void RefillEnergy(float amount)
		{
			CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, MaxEnergy);
		}

		protected virtual IEnumerator SpecialAttackSequence()
		{
			IsSpecialAttacking = true;
			LockHorizontalMovement();
			PlayAbilityStartFeedbacks();
			TriggerSpecialAnimator();

			yield return new WaitForSeconds(Mathf.Max(0f, SpecialAttackData.StartupTime));

			if (SpecialHitbox != null)
			{
				SpecialHitbox.Configure(SpecialAttackData, _character.gameObject, _character.IsFacingRight);
				SpecialHitbox.Activate();
			}

			yield return new WaitForSeconds(Mathf.Max(0f, SpecialAttackData.ActiveTime));

			if (SpecialHitbox != null)
			{
				SpecialHitbox.Deactivate();
			}

			yield return new WaitForSeconds(Mathf.Max(0f, SpecialAttackData.RecoveryTime));

			StopSpecialAttack();
		}

		public virtual void StopSpecialAttack()
		{
			if (SpecialHitbox != null)
			{
				SpecialHitbox.Deactivate();
			}

			IsSpecialAttacking = false;
			UnlockHorizontalMovement();
			StopStartFeedbacks();
			PlayAbilityStopFeedbacks();
			_attackCoroutine = null;
			_lastUseTime = Time.time;
		}

		protected virtual void LockHorizontalMovement()
		{
			if (!PreventHorizontalMovement || _horizontalMovement == null)
			{
				return;
			}

			_movementWasForbidden = _horizontalMovement.MovementForbidden;
			_horizontalMovement.SetHorizontalMove(0f);
			_horizontalMovement.MovementForbidden = true;
			_controller?.SetHorizontalForce(0f);
		}

		protected virtual void UnlockHorizontalMovement()
		{
			if (!PreventHorizontalMovement || _horizontalMovement == null)
			{
				return;
			}

			_horizontalMovement.MovementForbidden = _movementWasForbidden;
		}

		protected virtual void TriggerSpecialAnimator()
		{
			if ((_animator == null) || (_character == null) || (SpecialAttackData == null))
			{
				return;
			}

			MMAnimatorExtensions.UpdateAnimatorInteger(_animator, _specialIndexParameter, SpecialAttackData.SpecialIndex, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorTrigger(_animator, _specialAttackTriggerParameter, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		protected virtual bool SpecialInputDown()
		{
			switch (InputSource)
			{
				case SpecialInputSources.SecondaryShoot:
					return (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
					       || (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonDown);
				default:
					return _inputManager.TimeControlButton.State.CurrentState == MMInput.ButtonStates.ButtonDown;
			}
		}

		public override void ResetAbility()
		{
			base.ResetAbility();

			if (_attackCoroutine != null)
			{
				StopCoroutine(_attackCoroutine);
			}

			StopSpecialAttack();
		}

		/// <summary>
		/// If the ability is disabled mid-sequence (dialog freeze, generic all-ability
		/// disable, respawn), the coroutine below dies before it can reach
		/// StopSpecialAttack - leaving MovementForbidden stuck true and the player
		/// gliding with no walking animation. Release the lock on disable instead.
		/// </summary>
		protected virtual void OnDisable()
		{
			base.OnDisable();

			if (_attackCoroutine != null)
			{
				StopCoroutine(_attackCoroutine);
				_attackCoroutine = null;
			}

			if (IsSpecialAttacking)
			{
				UnlockHorizontalMovement();
				IsSpecialAttacking = false;
			}
		}

		protected virtual void OnDrawGizmosSelected()
		{
			if (SpecialAttackData == null || SpecialHitbox == null)
			{
				return;
			}

			bool facingRight = !Application.isPlaying || _character == null || _character.IsFacingRight;
			Vector2 offset = SpecialAttackData.HitboxOffset;
			offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);

			Matrix4x4 previousMatrix = Gizmos.matrix;
			Color previousColor = Gizmos.color;
			Gizmos.matrix = SpecialHitbox.transform.localToWorldMatrix;

			Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.18f);
			Gizmos.DrawCube(offset, SpecialAttackData.HitboxSize);
			Gizmos.color = new Color(1f, 0.25f, 0.1f, 1f);
			Gizmos.DrawWireCube(offset, SpecialAttackData.HitboxSize);

			Gizmos.matrix = previousMatrix;
			Gizmos.color = previousColor;
		}
	}
}
