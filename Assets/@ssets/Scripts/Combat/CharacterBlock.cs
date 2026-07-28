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

        public bool IsBlocking { get; protected set; }

        protected const string BlockingAnimationParameterName = "Blocking";
        protected int _blockingAnimationParameter;
        protected float _previousAbilityMovementMultiplier = 1f;
        protected MaskboundInControlInputManager _maskboundInputManager;
        protected CharacterSkillCaster _skillCaster;

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
                (_condition.CurrentState != CharacterStates.CharacterConditions.Normal) ||
                (GroundedOnly && !_controller.State.IsGrounded) ||
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

            if (PreventHorizontalMovement && (_characterHorizontalMovement != null))
            {
                _previousAbilityMovementMultiplier = _characterHorizontalMovement.AbilityMovementSpeedMultiplier;
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
                _characterHorizontalMovement.AbilityMovementSpeedMultiplier = _previousAbilityMovementMultiplier;
            }

            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                BlockingAnimationParameterName,
                AnimatorControllerParameterType.Bool,
                out _blockingAnimationParameter);
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
            BlockStop();
        }

        protected override void OnDeath()
        {
            BlockStop();
            base.OnDeath();
        }

        protected override void OnDisable()
        {
            BlockStop();
            base.OnDisable();
        }
    }
}
