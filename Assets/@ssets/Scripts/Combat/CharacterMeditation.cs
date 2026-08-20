using MaskboundJinosi.Input;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Character/Abilities/Character Meditation")]
    public class CharacterMeditation : CharacterAbility
    {
        [Header("Meditation")]
        [Tooltip("If true, meditation can only start while grounded.")]
        public bool GroundedOnly = true;

        [Tooltip("Stops horizontal movement while meditating.")]
        public bool PreventHorizontalMovement = true;

        public bool IsMeditating { get; protected set; }

        protected const string MeditatingAnimationParameterName = "Meditating";
        protected const string MeditateStartAnimationParameterName = "MeditateStart";
        protected int _meditatingAnimationParameter;
        protected int _meditateStartAnimationParameter;
        protected float _previousAbilityMovementMultiplier = 1f;
        protected MaskboundInControlInputManager _maskboundInputManager;
        protected bool _movementLockedByMeditate;

        public override string HelpBoxText()
        {
            return "Hold the Meditate input to enter meditation pose. Freezes movement like block.";
        }

        protected override void Initialization()
        {
            base.Initialization();
            ResolveInputManager();
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
            if (_maskboundInputManager?.MeditateButton == null)
            {
                return;
            }

            MMInput.ButtonStates state = _maskboundInputManager.MeditateButton.State.CurrentState;
            if ((state == MMInput.ButtonStates.ButtonDown) ||
                (state == MMInput.ButtonStates.ButtonPressed))
            {
                MeditateStart();
            }
            else if ((state == MMInput.ButtonStates.ButtonUp) ||
                     (state == MMInput.ButtonStates.Off))
            {
                MeditateStop();
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (!IsMeditating)
            {
                return;
            }

            if (!AbilityAuthorized || ShouldInterruptMeditation())
            {
                MeditateStop();
                return;
            }

            if (PreventHorizontalMovement)
            {
                _controller.SetHorizontalForce(0f);
            }
        }

        protected virtual bool ShouldInterruptMeditation()
        {
            CharacterStates.CharacterConditions condition = _condition.CurrentState;
            return condition == CharacterStates.CharacterConditions.Dead ||
                   condition == CharacterStates.CharacterConditions.Stunned ||
                   condition == CharacterStates.CharacterConditions.Frozen ||
                   condition == CharacterStates.CharacterConditions.Paused;
        }

        public virtual void MeditateStart()
        {
            if (IsMeditating || !AbilityAuthorized)
            {
                return;
            }

            if ((_condition.CurrentState != CharacterStates.CharacterConditions.Normal) ||
                (GroundedOnly && !_controller.State.IsGrounded))
            {
                return;
            }

            IsMeditating = true;

            MMAnimatorExtensions.UpdateAnimatorTrigger(
                _animator,
                _meditateStartAnimationParameter,
                _character._animatorParameters,
                _character.PerformAnimatorSanityChecks);

            if (PreventHorizontalMovement && (_characterHorizontalMovement != null))
            {
                if (!_movementLockedByMeditate)
                {
                    _previousAbilityMovementMultiplier = _characterHorizontalMovement.AbilityMovementSpeedMultiplier;
                    _movementLockedByMeditate = true;
                }

                _characterHorizontalMovement.AbilityMovementSpeedMultiplier = 0f;
                _controller.SetHorizontalForce(0f);
            }

            PlayAbilityStartFeedbacks();
        }

        public virtual void MeditateStop()
        {
            if (!IsMeditating)
            {
                return;
            }

            IsMeditating = false;

            if (PreventHorizontalMovement && (_characterHorizontalMovement != null))
            {
                ReleaseMeditationMovement();
            }

            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
        }

        protected virtual void ReleaseMeditationMovement()
        {
            if (_movementLockedByMeditate && _characterHorizontalMovement != null)
            {
                _characterHorizontalMovement.AbilityMovementSpeedMultiplier = _previousAbilityMovementMultiplier;
            }

            _movementLockedByMeditate = false;
        }

        protected virtual void StopMeditationImmediately()
        {
            MeditateStop();
            ReleaseMeditationMovement();
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                MeditatingAnimationParameterName,
                AnimatorControllerParameterType.Bool,
                out _meditatingAnimationParameter);
            RegisterAnimatorParameter(
                MeditateStartAnimationParameterName,
                AnimatorControllerParameterType.Trigger,
                out _meditateStartAnimationParameter);
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator,
                _meditatingAnimationParameter,
                IsMeditating,
                _character._animatorParameters,
                _character.PerformAnimatorSanityChecks);
        }

        public override void ResetAbility()
        {
            base.ResetAbility();
            StopMeditationImmediately();
        }

        protected override void OnDeath()
        {
            StopMeditationImmediately();
            base.OnDeath();
        }

        protected override void OnDisable()
        {
            StopMeditationImmediately();
            base.OnDisable();
        }
    }
}
