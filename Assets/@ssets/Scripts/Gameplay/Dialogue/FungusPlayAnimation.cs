using Fungus;
using MoreMountains.CorgiEngine;
using MaskboundJinosi.UI;
using UnityEngine;
using CorgiCharacter = MoreMountains.CorgiEngine.Character;

namespace MaskboundJinosi.Gameplay.Dialogue
{
    /// <summary>
    /// Fungus command that plays an animator parameter (trigger/bool/int/float) on
    /// the player or the current boss. Targets are found at runtime, so this works
    /// with runtime-spawned characters without assigning scene references.
    /// </summary>
    [CommandInfo("Maskbound",
                 "Play Animation",
                 "Sets an animator parameter (Trigger/Bool/Int/Float) on the player or the current boss. Targets are auto-found at runtime.")]
    [AddComponentMenu("")]
    public class FungusPlayAnimation : Command
    {
        public enum AnimationTarget
        {
            Player,
            Boss
        }

        public enum ParameterType
        {
            Trigger,
            Bool,
            Int,
            Float
        }

        [Tooltip("Which character's animator to drive.")]
        [SerializeField] protected AnimationTarget target;

        [Tooltip("How the parameter is written.")]
        [SerializeField] protected ParameterType parameterType = ParameterType.Trigger;

        [Tooltip("Name of the animator parameter (e.g. 'Attack 1', 'Dashing', 'SkillIndex').")]
        [SerializeField] protected string parameterName = "";

        [Tooltip("Value used for Bool/Int/Float parameters.")]
        [SerializeField] protected float parameterValue;

        [Tooltip("Optional override: GameObject name to search when Target = Boss. Empty = use BossHealthTarget.Current.")]
        [SerializeField] protected string bossObjectName = "PrabuKlana";

        [Tooltip("Optional delay in seconds before the parameter is applied.")]
        [SerializeField] protected float delay;

        public override void OnEnter()
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                Continue();
                return;
            }

            if (Mathf.Approximately(delay, 0f))
            {
                ApplyAnimation();
            }
            else
            {
                StartCoroutine(ApplyAfterDelay());
            }

            Continue();
        }

        private System.Collections.IEnumerator ApplyAfterDelay()
        {
            yield return new WaitForSeconds(delay);
            ApplyAnimation();
        }

        private void ApplyAnimation()
        {
            Animator animator = ResolveAnimator();
            if (animator == null)
            {
                Debug.LogWarning($"[FungusPlayAnimation] No animator found for target '{target}'.", this);
                return;
            }

            int hash = Animator.StringToHash(parameterName);
            if (!HasParameter(animator, parameterName))
            {
                Debug.LogWarning($"[FungusPlayAnimation] Animator of '{animator.gameObject.name}' has no parameter '{parameterName}'.", this);
                return;
            }

            switch (parameterType)
            {
                case ParameterType.Trigger:
                    animator.ResetTrigger(hash);
                    animator.SetTrigger(hash);
                    break;
                case ParameterType.Bool:
                    animator.SetBool(hash, Mathf.Abs(parameterValue) > 0.01f);
                    break;
                case ParameterType.Int:
                    animator.SetInteger(hash, Mathf.RoundToInt(parameterValue));
                    break;
                case ParameterType.Float:
                    animator.SetFloat(hash, parameterValue);
                    break;
            }

            Debug.Log($"[FungusPlayAnimation] {target}: set '{parameterType}' '{parameterName}' on '{animator.gameObject.name}'.", this);
        }

        private Animator ResolveAnimator()
        {
            switch (target)
            {
                case AnimationTarget.Player:
                    return ResolvePlayerAnimator();
                case AnimationTarget.Boss:
                    return ResolveBossAnimator();
                default:
                    return null;
            }
        }

        private Animator ResolvePlayerAnimator()
        {
            CorgiCharacter player = GetMainPlayer();
            if (player == null)
            {
                return null;
            }

            return player.CharacterAnimator;
        }

        private CorgiCharacter GetMainPlayer()
        {
            if (LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
            {
                return LevelManager.Instance.Players[0];
            }

            // Fallback: find any active Character tagged "Player".
            CorgiCharacter[] characters = FindObjectsByType<CorgiCharacter>(FindObjectsSortMode.None);
            foreach (CorgiCharacter character in characters)
            {
                if (character.CompareTag("Player"))
                {
                    return character;
                }
            }

            return null;
        }

        private Animator ResolveBossAnimator()
        {
            // Prefer the statically-registered boss (BossHealthTarget.Current), same as BossHealthUIBinder.
            if (BossHealthTarget.Current != null && BossHealthTarget.Current.gameObject != null)
            {
                Animator animator = BossHealthTarget.Current.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    return animator;
                }
            }

            // Fallback: find by object name (e.g. "PrabuKlana"), same as DemoBossChallengeTimer.
            if (!string.IsNullOrEmpty(bossObjectName))
            {
                Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Animator animator in animators)
                {
                    Transform root = animator.transform.root;
                    if (animator.gameObject.name == bossObjectName ||
                        (root != null && root.name == bossObjectName))
                    {
                        return animator;
                    }
                }
            }

            return null;
        }

        private static bool HasParameter(Animator animator, string name)
        {
            for (int i = 0; i < animator.parameterCount; i++)
            {
                if (animator.parameters[i].name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public override string GetSummary()
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                return "Error: No parameter name";
            }

            return $"{target}: {parameterType} {parameterName}" +
                   (parameterType == ParameterType.Bool || parameterType == ParameterType.Int || parameterType == ParameterType.Float
                       ? " = " + parameterValue
                       : "");
        }

        public override Color GetButtonColor()
        {
            return new Color32(255, 200, 150, 255);
        }
    }
}
