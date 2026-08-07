using System.Collections;
using MaskboundJinosi.Input;
using MaskboundJinosi.Skills;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Character/Abilities/Character Block")]
    public class CharacterBlock : CharacterAbility
    {
        [Header("Block")]
        [Tooltip("Damage resistance on a dedicated child GameObject. Keep that child inactive when not blocking.")]
        public DamageResistance BlockingResistance;

        [Tooltip("If enabled, blocking can only start and remain active while grounded.")]
        public bool GroundedOnly = true;

        [Tooltip("Stops horizontal movement while the character is blocking.")]
        public bool PreventHorizontalMovement = true;

		[Tooltip("Fallback delay before movement returns after block is released. Match this to the release animation length, or use the animation event method.")]
		[Min(0f)] public float MovementReleaseDelay = 0.27f;

        public bool IsBlocking { get; protected set; }

        protected const string BlockingAnimationParameterName = "Blocking";
        protected const string BlockStartAnimationParameterName = "BlockStart";
        protected int _blockingAnimationParameter;
        protected int _blockStartAnimationParameter;
        protected float _previousAbilityMovementMultiplier = 1f;
        protected MaskboundInControlInputManager _maskboundInputManager;
        protected CharacterSkillCaster _skillCaster;
		protected Coroutine _movementReleaseCoroutine;
		protected bool _movementLockedByBlock;

        public override string HelpBoxText()
        {
            return "Hold the Maskbound Block input to enable a Corgi DamageResistance and the Blocking animator parameter.";
        }

        protected override void Initialization()
        {
            base.Initialization();
            ResolveInputManager();

            _skillCaster = _character?.GetComponentInChildren<CharacterSkillCaster>(true);

            if (BlockingResistance != null)
            {
                BlockingResistance.gameObject.SetActive(false);
            }
        }

        protected virtual bool IsAttacking()
        {
            if (_handleWeaponList == null)
            {
                return false;
            }

            foreach (CharacterHandleWeapon handleWeapon in _handleWeaponList)
            {
                if ((handleWeapon.CurrentWeapon != null)
                    && (handleWeapon.CurrentWeapon.WeaponState.CurrentState != Weapon.WeaponStates.WeaponIdle))
                {
                    return true;
                }
            }

            return false;
        }

        public override void SetInputManager(InputManager inputManager)
        {
            base.SetInputManager(inputManager);
            ResolveInputManager();
        }

        protected virtual void ResolveInputManager()
        {
            _maskboundInputManager = _inputManager as MaskboundInControlInputManager;
        }

        protected override void HandleInput()
        {
            if (_maskboundInputManager?.BlockButton == null)
            {
                return;
            }

            MMInput.ButtonStates state = _maskboundInputManager.BlockButton.State.CurrentState;
            if ((state == MMInput.ButtonStates.ButtonDown) ||
                (state == MMInput.ButtonStates.ButtonPressed))
            {
                BlockStart();
            }
            else if ((state == MMInput.ButtonStates.ButtonUp) ||
                     (state == MMInput.ButtonStates.Off))
            {
                BlockStop();
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (!IsBlocking)
            {
                return;
            }

            if (!AbilityAuthorized ||
                ShouldInterruptActiveBlock() ||
                ((_skillCaster != null) && _skillCaster.IsCasting) ||
                IsAttacking())
            {
                BlockStop();
                return;
            }

            if (PreventHorizontalMovement)
            {
                _controller.SetHorizontalForce(0f);
            }

        }

        /// <summary>
        /// Damage and knockback may temporarily put the character in ControlledMovement
        /// or lift it off the ground. Those states must not release a held block.
        /// GroundedOnly is intentionally checked by BlockStart only.
        /// </summary>
        protected virtual bool ShouldInterruptActiveBlock()
        {
            CharacterStates.CharacterConditions condition = _condition.CurrentState;
            return condition == CharacterStates.CharacterConditions.Dead ||
                   condition == CharacterStates.CharacterConditions.Stunned ||
                   condition == CharacterStates.CharacterConditions.Frozen ||
                   condition == CharacterStates.CharacterConditions.Paused;
        }

        public virtual void BlockStart()
        {
            if (IsBlocking || !AbilityAuthorized || BlockingResistance == null)
            {
                return;
            }

            if ((_condition.CurrentState != CharacterStates.CharacterConditions.Normal) ||
                (GroundedOnly && !_controller.State.IsGrounded) ||
                ((_skillCaster != null) && _skillCaster.IsCasting) ||
                IsAttacking())
            {
                return;
            }

            IsBlocking = true;
            _character.IsBlocking = true;
            BlockingResistance.gameObject.SetActive(true);
            MMAnimatorExtensions.UpdateAnimatorTrigger(
                _animator,
                _blockStartAnimationParameter,
                _character._animatorParameters,
                _character.PerformAnimatorSanityChecks);

            if (PreventHorizontalMovement && (_characterHorizontalMovement != null))
            {
				if (_movementReleaseCoroutine != null)
				{
					StopCoroutine(_movementReleaseCoroutine);
					_movementReleaseCoroutine = null;
				}

				if (!_movementLockedByBlock)
				{
					_previousAbilityMovementMultiplier = _characterHorizontalMovement.AbilityMovementSpeedMultiplier;
					_movementLockedByBlock = true;
				}

                _characterHorizontalMovement.AbilityMovementSpeedMultiplier = 0f;
                _controller.SetHorizontalForce(0f);
            }

            PlayAbilityStartFeedbacks();
        }

        public virtual void BlockStop()
        {
            if (!IsBlocking)
            {
                return;
            }

            IsBlocking = false;
            _character.IsBlocking = false;

            if (BlockingResistance != null)
            {
                BlockingResistance.gameObject.SetActive(false);
            }

            if (PreventHorizontalMovement && (_characterHorizontalMovement != null))
            {
				if (MovementReleaseDelay > 0f)
				{
					_movementReleaseCoroutine = StartCoroutine(ReleaseMovementAfterDelay());
				}
				else
				{
					ReleaseBlockMovement();
				}
            }

            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
        }

		protected virtual IEnumerator ReleaseMovementAfterDelay()
		{
			yield return new WaitForSeconds(MovementReleaseDelay);
			_movementReleaseCoroutine = null;
			ReleaseBlockMovement();
		}

		/// <summary>Animation Event target for the final frame of ReleaseBlockPose.</summary>
		public virtual void ReleaseBlockMovement()
		{
			if (_movementReleaseCoroutine != null)
			{
				StopCoroutine(_movementReleaseCoroutine);
				_movementReleaseCoroutine = null;
			}

			if (_movementLockedByBlock && _characterHorizontalMovement != null)
			{
				_characterHorizontalMovement.AbilityMovementSpeedMultiplier = _previousAbilityMovementMultiplier;
			}

			_movementLockedByBlock = false;
		}

		protected virtual void StopBlockImmediately()
		{
			BlockStop();
			ReleaseBlockMovement();
		}

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                BlockingAnimationParameterName,
                AnimatorControllerParameterType.Bool,
                out _blockingAnimationParameter);
            RegisterAnimatorParameter(
                BlockStartAnimationParameterName,
                AnimatorControllerParameterType.Trigger,
                out _blockStartAnimationParameter);
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator,
                _blockingAnimationParameter,
                IsBlocking,
                _character._animatorParameters,
                _character.PerformAnimatorSanityChecks);
        }

        public override void ResetAbility()
        {
            base.ResetAbility();
			StopBlockImmediately();
        }

        protected override void OnDeath()
        {
			StopBlockImmediately();
            base.OnDeath();
        }

        protected override void OnDisable()
        {
			StopBlockImmediately();
            base.OnDisable();
        }
    }
}
