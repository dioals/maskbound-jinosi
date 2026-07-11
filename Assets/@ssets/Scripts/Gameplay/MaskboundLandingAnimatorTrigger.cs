using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Maskbound/Gameplay/Landing Animator Trigger")]
    public class MaskboundLandingAnimatorTrigger : MonoBehaviour
    {
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private string landingTriggerName = "Landing";
        [SerializeField] private float minimumAirTime = 0.15f;
        [SerializeField] private float minimumFallSpeed = 1f;
        [SerializeField] private float triggerCooldown = 0.1f;

        private CorgiController _controller;
        private int _landingTriggerHash;
        private float _lastAirTime;
        private float _lastVerticalSpeed;
        private float _lastTriggerTime = -999f;

        private void Reset()
        {
            targetAnimator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            _controller = GetComponent<CorgiController>();

            if (targetAnimator == null)
            {
                targetAnimator = GetComponentInChildren<Animator>();
            }

            _landingTriggerHash = Animator.StringToHash(landingTriggerName);
        }

        private void Update()
        {
            if (_controller == null || targetAnimator == null)
            {
                return;
            }

            if (!_controller.State.IsGrounded)
            {
                _lastAirTime = _controller.TimeAirborne;
                _lastVerticalSpeed = _controller.Speed.y;
                return;
            }

            if (!_controller.State.JustGotGrounded)
            {
                return;
            }

            if (_lastAirTime < minimumAirTime)
            {
                return;
            }

            if (_lastVerticalSpeed > -minimumFallSpeed)
            {
                return;
            }

            if (Time.time - _lastTriggerTime < triggerCooldown)
            {
                return;
            }

            _lastTriggerTime = Time.time;
            targetAnimator.ResetTrigger(_landingTriggerHash);
            targetAnimator.SetTrigger(_landingTriggerHash);
        }
    }
}
