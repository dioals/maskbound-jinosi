using System.Collections;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    /// <summary>
    /// Restores a Corgi AI Brain after Character.OnDeath disables it, then moves the
    /// brain into a dedicated death state so its final AI actions can run.
    /// </summary>
    [AddComponentMenu("Maskbound/AI/Boss Death Brain State")]
    [RequireComponent(typeof(Health))]
    public class BossDeathBrainState : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private AIBrain brain;

        [Header("Death State")]
        [SerializeField] private string deathStateName = "Die";
        [SerializeField] private bool logTransition;

        private Coroutine _transitionCoroutine;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (health != null)
            {
                health.OnDeath -= HandleDeath;
                health.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
            }

            _transitionCoroutine = null;
        }

        private void HandleDeath()
        {
            if (_transitionCoroutine == null && isActiveAndEnabled)
            {
                _transitionCoroutine = StartCoroutine(EnterDeathStateAfterCorgiCleanup());
            }
        }

        private IEnumerator EnterDeathStateAfterCorgiCleanup()
        {
            // Character.OnDeath runs from the same Health.OnDeath event and disables
            // CharacterBrain. Waiting one frame ensures that cleanup has completed.
            yield return null;

            if (brain == null)
            {
                Debug.LogError("BossDeathBrainState couldn't find an AIBrain on this boss.", this);
                _transitionCoroutine = null;
                yield break;
            }

            if (!HasState(deathStateName))
            {
                Debug.LogError($"AI Brain doesn't contain a state named '{deathStateName}'.", this);
                _transitionCoroutine = null;
                yield break;
            }

            brain.enabled = true;
            brain.BrainActive = true;
            brain.TransitionToState(deathStateName);

            if (logTransition)
            {
                Debug.Log($"Boss AI entered death state '{deathStateName}'.", this);
            }

            _transitionCoroutine = null;
        }

        private bool HasState(string stateName)
        {
            if (brain.States == null || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            for (int i = 0; i < brain.States.Count; i++)
            {
                if (brain.States[i] != null && brain.States[i].StateName == stateName)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (brain == null)
            {
                brain = GetComponent<AIBrain>();
            }
        }

        private void Reset()
        {
            ResolveReferences();
        }
    }
}
