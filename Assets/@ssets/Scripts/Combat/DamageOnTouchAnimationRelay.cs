using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Combat/Damage On Touch Animation Relay")]
    public class DamageOnTouchAnimationRelay : MonoBehaviour
    {
        [Tooltip("DamageOnTouch yang akan dikontrol. Kosongkan untuk mencarinya otomatis pada object ini dan child.")]
        [SerializeField] private DamageOnTouch[] targets;
        [SerializeField] private bool includeInactiveChildren = true;
        [Tooltip("Memastikan hitbox damage mati sebelum Animation Event EnableDamage dipanggil.")]
        [SerializeField] private bool disableOnAwake = true;
        [SerializeField] private bool logMissingTargets;

        private void Awake()
        {
            ResolveTargets();

            if (disableOnAwake)
            {
                SetDamageEnabled(false);
            }
        }

        public void EnableDamage()
        {
            SetDamageEnabled(true);
        }

        public void DisableDamage()
        {
            SetDamageEnabled(false);
        }

        // Animation Event dapat mengirim int: 0 = nonaktif, selain 0 = aktif.
        public void SetDamageEnabled(int enabled)
        {
            SetDamageEnabled(enabled != 0);
        }

        public void SetDamageEnabled(bool enabled)
        {
            ResolveTargets();

            if (targets == null || targets.Length == 0)
            {
                if (logMissingTargets)
                {
                    Debug.LogWarning($"{name} tidak menemukan komponen DamageOnTouch.", this);
                }
                return;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].enabled = enabled;
                }
            }
        }

        private void ResolveTargets()
        {
            if (targets != null && targets.Length > 0)
            {
                return;
            }

            targets = GetComponentsInChildren<DamageOnTouch>(includeInactiveChildren);
            if (targets.Length == 0)
            {
                DamageOnTouch parentTarget = GetComponentInParent<DamageOnTouch>();
                if (parentTarget != null)
                {
                    targets = new[] { parentTarget };
                }
            }
        }

        private void OnDisable()
        {
            SetDamageEnabled(false);
        }
    }
}
