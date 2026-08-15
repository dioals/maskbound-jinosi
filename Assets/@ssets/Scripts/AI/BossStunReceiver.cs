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

        private Coroutine _stunRoutine;
        private bool _brainWasActive;

        private void Awake()
        {
            characterStun ??= GetComponent<CharacterStun>();
            brain ??= GetComponent<AIBrain>();
        }

        public void StunFor(float duration)
        {
            if (!isActiveAndEnabled || duration <= 0f) { return; }
            if (_stunRoutine != null) { StopCoroutine(_stunRoutine); }
            _stunRoutine = StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            if (brain != null)
            {
                _brainWasActive = brain.BrainActive;
                brain.BrainActive = false;
            }

            characterStun?.Stun();
            yield return new WaitForSeconds(duration);
            characterStun?.ExitStun();

            if (brain != null && _brainWasActive)
            {
                brain.BrainActive = true;
            }
            _stunRoutine = null;
        }

        private void OnDisable()
        {
            if (brain != null && _brainWasActive) { brain.BrainActive = true; }
        }
    }
}
