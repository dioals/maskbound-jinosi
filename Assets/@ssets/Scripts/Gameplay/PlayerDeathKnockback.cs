using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
    /// <summary>
    /// Applies a short death knockback away from the damage source, then removes the
    /// horizontal force so a dead character doesn't slide forever.
    /// </summary>
    [AddComponentMenu("Maskbound/Gameplay/Player Death Knockback")]
    [RequireComponent(typeof(Character), typeof(Health), typeof(CorgiController))]
    public class PlayerDeathKnockback : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float horizontalForce = 7f;
        [SerializeField, Min(0f)] private float verticalForce = 12f;
        [SerializeField, Min(0f)] private float horizontalForceDuration = 0.35f;

        private Character _character;
        private Health _health;
        private CorgiController _controller;
        private Coroutine _stopCoroutine;

        private void Awake()
        {
            _character = GetComponent<Character>();
            _health = GetComponent<Health>();
            _controller = GetComponent<CorgiController>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnDeath -= HandleDeath;
            }

            if (_stopCoroutine != null)
            {
                StopCoroutine(_stopCoroutine);
                _stopCoroutine = null;
            }
        }

        private void HandleDeath()
        {
            float direction = ResolveKnockbackDirection();

            // Health.Kill invokes OnDeath before applying DeathForce, so changing these
            // values here affects the force that Corgi applies later in the same frame.
            _health.ApplyDeathForce = true;
            _health.ResetForcesOnDeath = true;
            _health.GravityOffOnDeath = false;
            _health.DeathForce = new Vector2(direction * horizontalForce, verticalForce);

            if (_stopCoroutine != null)
            {
                StopCoroutine(_stopCoroutine);
            }

            _stopCoroutine = StartCoroutine(StopHorizontalForceAfterDelay());
        }

        private float ResolveKnockbackDirection()
        {
            float damageDirectionX = _health.LastDamageDirection.x;
            if (Mathf.Abs(damageDirectionX) > 0.01f)
            {
                // LastDamageDirection points from the attacker towards the victim.
                return Mathf.Sign(damageDirectionX);
            }

            // Fallback: throw the player backwards relative to their facing direction.
            return _character.IsFacingRight ? -1f : 1f;
        }

        private IEnumerator StopHorizontalForceAfterDelay()
        {
            if (horizontalForceDuration > 0f)
            {
                yield return new WaitForSeconds(horizontalForceDuration);
            }

            _controller.SetHorizontalForce(0f);

            // Character.HandleCharacterStatus automatically keeps horizontal force at
            // zero while dead whenever DeathForce.x is zero.
            Vector2 deathForce = _health.DeathForce;
            deathForce.x = 0f;
            _health.DeathForce = deathForce;
            _stopCoroutine = null;
        }
    }
}
