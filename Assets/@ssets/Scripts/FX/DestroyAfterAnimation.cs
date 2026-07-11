using UnityEngine;

namespace Maskbound.FX
{
    [DisallowMultipleComponent]
    public class DestroyAfterAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float fallbackLifetime = 1f;
        [SerializeField] private bool destroyOnStart = true;

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            if (!destroyOnStart)
            {
                return;
            }

            Destroy(gameObject, GetLifetime());
        }

        public void DestroyNow()
        {
            Destroy(gameObject);
        }

        private float GetLifetime()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return fallbackLifetime;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
            {
                return fallbackLifetime;
            }

            float longestClipLength = 0f;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].length > longestClipLength)
                {
                    longestClipLength = clips[i].length;
                }
            }

            return longestClipLength > 0f ? longestClipLength : fallbackLifetime;
        }
    }
}
