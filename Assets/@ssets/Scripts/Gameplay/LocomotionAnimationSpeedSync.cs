using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    /// <summary>
    /// Scales the Walk-Run animator state's playback speed to match the character's actual
    /// horizontal speed, so faster movement (e.g. Running) doesn't look like feet are sliding/dragging.
    /// Only states with "LocomotionSpeedMultiplier" enabled as their Speed parameter are affected.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Maskbound/Gameplay/Locomotion Animation Speed Sync")]
    public class LocomotionAnimationSpeedSync : MonoBehaviour
    {
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private CharacterHorizontalMovement horizontalMovement;
        [SerializeField] private CorgiController controller;
        [SerializeField] private string speedMultiplierParameterName = "LocomotionSpeedMultiplier";

        [Tooltip("Floor for the animator speed multiplier, so locomotion doesn't visibly freeze while decelerating to a stop.")]
        [SerializeField] private float minimumSpeedMultiplier = 0.3f;
        [Tooltip("Ceiling for the animator speed multiplier, in case of speed bursts (knockback, dashes, etc).")]
        [SerializeField] private float maximumSpeedMultiplier = 3f;

        private int _speedMultiplierParameterHash;

        private void Reset()
        {
            targetAnimator = GetComponentInChildren<Animator>();
            horizontalMovement = GetComponentInParent<CharacterHorizontalMovement>();
            controller = GetComponentInParent<CorgiController>();
        }

        private void Awake()
        {
            if (targetAnimator == null)
            {
                targetAnimator = GetComponentInChildren<Animator>();
            }

            if (horizontalMovement == null)
            {
                horizontalMovement = GetComponentInParent<CharacterHorizontalMovement>();
            }

            if (controller == null)
            {
                controller = GetComponentInParent<CorgiController>();
            }

            _speedMultiplierParameterHash = Animator.StringToHash(speedMultiplierParameterName);
        }

        private void Update()
        {
            if (targetAnimator == null || horizontalMovement == null || controller == null || horizontalMovement.WalkSpeed <= 0f)
            {
                return;
            }

            // WalkSpeed is used as the fixed reference the Walk-Run clips were authored at, so
            // Running (a higher MovementSpeed) plays proportionally faster instead of at the same 1x rate.
            float rawMultiplier = Mathf.Abs(controller.Speed.x) / horizontalMovement.WalkSpeed;
            float multiplier = rawMultiplier <= 0f ? 0f : Mathf.Clamp(rawMultiplier, minimumSpeedMultiplier, maximumSpeedMultiplier);

            targetAnimator.SetFloat(_speedMultiplierParameterHash, multiplier);
        }
    }
}
