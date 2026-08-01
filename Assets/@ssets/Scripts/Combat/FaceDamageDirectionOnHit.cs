using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Combat/Face Damage Direction On Hit")]
    public class FaceDamageDirectionOnHit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private Character character;

        [Header("Facing")]
        [SerializeField] private bool faceTowardsDamageSource = true;
        [SerializeField, Min(0f)] private float holdFacingDuration = 0.05f;

        private float _holdFacingUntil;
        private int _pendingFacing;

        private void Awake()
        {
            health ??= GetComponentInParent<Health>();
            character ??= GetComponentInParent<Character>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnHit += RequestFaceDamageDirection;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnHit -= RequestFaceDamageDirection;
            }
        }

        private void LateUpdate()
        {
            if (_pendingFacing == 0 || Time.time > _holdFacingUntil)
            {
                _pendingFacing = 0;
                return;
            }

            ApplyFacing(_pendingFacing);
        }

        private void RequestFaceDamageDirection()
        {
            if (health == null || character == null)
            {
                return;
            }

            float damageDirectionX = health.LastDamageDirection.x;
            if (Mathf.Approximately(damageDirectionX, 0f))
            {
                return;
            }

            _pendingFacing = faceTowardsDamageSource
                ? -Mathf.RoundToInt(Mathf.Sign(damageDirectionX))
                : Mathf.RoundToInt(Mathf.Sign(damageDirectionX));

            _holdFacingUntil = Time.time + holdFacingDuration;
            ApplyFacing(_pendingFacing);
        }

        private void ApplyFacing(int direction)
        {
            character.Face(direction > 0
                ? Character.FacingDirections.Right
                : Character.FacingDirections.Left);
        }
    }
}
