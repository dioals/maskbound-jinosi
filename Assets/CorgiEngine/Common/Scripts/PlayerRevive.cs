using System.Collections;
using UnityEngine;
using CorgiCharacter = MoreMountains.CorgiEngine.Character;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Plays the player's "Revive" animation once, right after the player is spawned.
    /// The player spawns (is repositioned) normally but is kept invisible for a short
    /// moment, then pops into view exactly as the revive animation starts. Player abilities
    /// are frozen while the animation plays so the locomotion animator cannot override it,
    /// and are restored once the animation finishes. The player is invulnerable for the
    /// whole revive, since it can neither be seen nor act during it.
    /// Triggered from a CheckPoint when its PlayReviveOnRespawn flag is enabled, so the
    /// revive is fully configurable per checkpoint.
    /// </summary>
    public static class PlayerRevive
    {
        private const string ReviveStateName = "Revive";
        private const string AliveParameterName = "Alive";
        private const string DamageTriggerName = "Damage";
        private const string DeathTriggerName = "Death";
        private const float ReviveAnimationDuration = 0.9f;

        /// <summary>
        /// Plays a revive on the player: hides it, waits <paramref name="delayBeforeVisible"/>
        /// seconds, makes it visible again and starts the "Revive" animation.
        /// </summary>
        /// <param name="player">The player character to revive.</param>
        /// <param name="delayBeforeVisible">Seconds the player stays invisible after spawning
        /// before it pops into view and the revive animation starts.</param>
        public static void Play(CorgiCharacter player, float delayBeforeVisible)
        {
            if (player == null)
            {
                return;
            }

            // Attach a tiny runner component that owns the coroutine, so it survives
            // independently of the checkpoint (which may get disabled or destroyed).
            ReviveRunner runner = player.gameObject.GetComponent<ReviveRunner>();
            if (runner == null)
            {
                runner = player.gameObject.AddComponent<ReviveRunner>();
            }

            runner.PlayRevive(delayBeforeVisible);
        }

        /// <summary>
        /// Runtime-only helper component attached to the player that runs the revive
        /// hide/visible/play/unfreeze coroutine.
        /// </summary>
        private class ReviveRunner : MonoBehaviour
        {
            // Snapshot of the player's state taken when a revive starts. Kept across a
            // restarted revive (e.g. two respawns in a row) so the second revive never
            // snapshots the already-frozen state and restores the player to "disabled".
            private bool _reviving;
            private bool _characterWasEnabled;
            private CharacterAbility[] _abilities;
            private bool[] _abilityStates;
            private Health _health;
            private bool _healthWasInvulnerable;

            public void PlayRevive(float delayBeforeVisible)
            {
                StopAllCoroutines();
                StartCoroutine(DoRevive(delayBeforeVisible));
            }

            private IEnumerator DoRevive(float delayBeforeVisible)
            {
                CorgiCharacter player = GetComponent<CorgiCharacter>();
                if (player == null)
                {
                    yield break;
                }

                Animator animator = player.CharacterAnimator;
                if (animator == null)
                {
                    yield break;
                }

                int stateHash = Animator.StringToHash(ReviveStateName);
                if (!animator.HasState(0, stateHash))
                {
                    yield break;
                }

                if (!_reviving)
                {
                    TakeSnapshot(player);
                    _reviving = true;
                }

                // Freeze the player so the locomotion animator cannot override the revive.
                player.enabled = false;
                for (int i = 0; i < _abilities.Length; i++)
                {
                    _abilities[i].enabled = false;
                }

                // The Character stops updating the animator while disabled, so its
                // parameters keep their values from the death frame (Alive = false).
                // Reset them, otherwise any Damage/Death trigger drives the animator
                // back into the Die state for the whole revive.
                ResetAnimatorForRevive(animator);

                // Invisible and unable to act: don't let anything hit the player meanwhile.
                if (_health != null)
                {
                    _health.DamageDisabled();
                }

                CorgiController controller = player.GetComponent<CorgiController>();
                if (controller != null)
                {
                    controller.SetHorizontalForce(0f);
                }

                SetVisible(player, false);

                // Keep the player invisible for the configured delay, then pop into view.
                yield return new WaitForSeconds(delayBeforeVisible);

                SetVisible(player, true);
                animator.Play(stateHash, 0, 0f);

                // Wait out the animation before handing back control to the player.
                yield return new WaitForSeconds(ReviveAnimationDuration);

                Restore(player);
            }

            private void TakeSnapshot(CorgiCharacter player)
            {
                _characterWasEnabled = player.enabled;
                _abilities = player.GetComponents<CharacterAbility>();
                _abilityStates = new bool[_abilities.Length];
                for (int i = 0; i < _abilities.Length; i++)
                {
                    _abilityStates[i] = _abilities[i].enabled;
                }

                _health = player.CharacterHealth;
                _healthWasInvulnerable = (_health != null) && _health.TemporarilyInvulnerable;
            }

            private void Restore(CorgiCharacter player)
            {
                player.enabled = _characterWasEnabled;
                for (int i = 0; i < _abilities.Length; i++)
                {
                    if (_abilities[i] != null)
                    {
                        _abilities[i].enabled = _abilityStates[i];
                    }
                }

                // Only lift the invulnerability we added, not one set by another system.
                if ((_health != null) && !_healthWasInvulnerable)
                {
                    _health.DamageEnabled();
                }

                _reviving = false;
                _abilities = null;
                _abilityStates = null;
                _health = null;
            }

            private static void ResetAnimatorForRevive(Animator animator)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == AliveParameterName && parameter.type == AnimatorControllerParameterType.Bool)
                    {
                        animator.SetBool(AliveParameterName, true);
                    }
                    else if ((parameter.name == DamageTriggerName || parameter.name == DeathTriggerName)
                             && parameter.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.ResetTrigger(parameter.name);
                    }
                }
            }

            /// <summary>
            /// Toggles every sprite renderer on the player (including children) so the whole
            /// character can be shown/hidden without deactivating behaviours.
            /// </summary>
            private static void SetVisible(CorgiCharacter player, bool visible)
            {
                SpriteRenderer[] renderers = player.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = visible;
                }
            }
        }
    }
}
