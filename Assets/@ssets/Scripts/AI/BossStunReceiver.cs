using System.Collections;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Boss Stun Receiver")]
    [RequireComponent(typeof(CharacterStun))]
    public class BossStunReceiver : MonoBehaviour
    {
        [SerializeField] private CharacterStun characterStun;
        [SerializeField] private AIBrain brain;

        [Header("Limits")]
        [Tooltip("Hard cap on a single stun, whatever duration the caller asks for.")]
        [SerializeField, Min(0f)] private float maximumStunDuration = 3f;
        [Tooltip("Minimum time between two stuns. Stun requests arriving sooner are ignored, so the boss can't be chain-locked.")]
        [SerializeField, Min(0f)] private float stunCooldown = 4f;

        private Coroutine _stunRoutine;
        private bool _stunned;
        private bool _brainWasActive;
        private float _lastStunEndedAt = -999f;

        private void Awake()
        {
            characterStun ??= GetComponent<CharacterStun>();
            brain ??= GetComponent<AIBrain>();
        }

        public void StunFor(float duration)
        {
            if (!isActiveAndEnabled || duration <= 0f) { return; }
            if (!_stunned && (Time.time - _lastStunEndedAt < stunCooldown)) { return; }

            if (maximumStunDuration > 0f)
            {
                duration = Mathf.Min(duration, maximumStunDuration);
            }

            if (_stunRoutine != null) { StopCoroutine(_stunRoutine); }
            _stunRoutine = StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            // Only capture the brain's pre-stun state on the first stun. A stun that lands
            // while the boss is already stunned would otherwise capture the deactivated
            // brain and never restore it, leaving the boss passive for the rest of the fight.
            if (brain != null && !_stunned)
            {
                _brainWasActive = brain.BrainActive;
            }

            _stunned = true;

            if (brain != null)
            {
                brain.BrainActive = false;
            }

            characterStun?.Stun();
            yield return new WaitForSeconds(duration);

            _stunRoutine = null;
            EndStun();
        }

        private void EndStun()
        {
            if (!_stunned) { return; }

            _stunned = false;
            _lastStunEndedAt = Time.time;
            characterStun?.ExitStun();

            if (brain != null && _brainWasActive)
            {
                brain.BrainActive = true;
            }
        }

        private void OnDisable()
        {
            if (_stunRoutine != null)
            {
                StopCoroutine(_stunRoutine);
                _stunRoutine = null;
            }

            EndStun();
        }
    }
}
