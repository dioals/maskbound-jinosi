using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    /// <summary>
    /// Animation Event helper for temporarily disabling horizontal movement during landing clips.
    /// </summary>
    public class LandingMovementLock : MonoBehaviour
    {
        [SerializeField] private CharacterHorizontalMovement horizontalMovement;
        [SerializeField] private float autoUnlockDelay = 0.35f;

        private float _unlockAt = -1f;
        private bool _isLocked;
        private bool _previousMovementForbidden;

        private void Awake()
        {
            if (horizontalMovement == null)
            {
                horizontalMovement = GetComponentInParent<CharacterHorizontalMovement>();
            }
        }

        private void Update()
        {
            if (_isLocked && autoUnlockDelay > 0f && Time.time >= _unlockAt)
            {
                EnableHorizontalMovement();
            }
        }

        public void DisableHorizontalMovement()
        {
            if (horizontalMovement == null)
            {
                return;
            }

            if (!_isLocked)
            {
                _previousMovementForbidden = horizontalMovement.MovementForbidden;
            }

            horizontalMovement.MovementForbidden = true;
            horizontalMovement.SetHorizontalMove(0f);
            _isLocked = true;
            _unlockAt = Time.time + autoUnlockDelay;
        }

        public void EnableHorizontalMovement()
        {
            if (horizontalMovement == null)
            {
                return;
            }

            horizontalMovement.MovementForbidden = _previousMovementForbidden;
            _isLocked = false;
            _unlockAt = -1f;
        }
    }
}
