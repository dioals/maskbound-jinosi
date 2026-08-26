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
    /// and are restored once the animation finishes.
    /// Triggered from a CheckPoint when its PlayReviveOnRespawn flag is enabled, so the
    /// revive is fully configurable per checkpoint.
    /// </summary>
    public static class PlayerRevive
    {
        private const string ReviveStateName = "Revive";
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

                CharacterAbility[] abilities = player.GetComponents<CharacterAbility>();
                bool[] abilityStates = new bool[abilities.Length];

                // Freeze the player so the locomotion animator cannot override the revive.
                bool characterWasEnabled = player.enabled;
                player.enabled = false;

                for (int i = 0; i < abilities.Length; i++)
                {
                    abilityStates[i] = abilities[i].enabled;
                    abilities[i].enabled = false;
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

                // Unfreeze.
                player.enabled = characterWasEnabled;
                for (int i = 0; i < abilities.Length; i++)
                {
                    abilities[i].enabled = abilityStates[i];
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