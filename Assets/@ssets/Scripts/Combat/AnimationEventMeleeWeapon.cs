using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
    [AddComponentMenu("Maskbound/Combat/Animation Event Melee Weapon")]
    public class AnimationEventMeleeWeapon : MeleeWeapon
    {
        [Header("Animation Event Damage Window")]
        [SerializeField] private bool useAnimationEventDamageWindow = true;
        [SerializeField] private bool autoCloseAfterOpen = true;
        [SerializeField, Min(0.01f)] private float damageWindowDuration = 0.2f;
        [SerializeField] private bool logDamageWindow;

        [Header("Hitstop")]
        [Tooltip("How long (in real seconds) to freeze the game when this attack lands a hit. 0 disables hitstop for this attack.")]
        [SerializeField, Min(0f)] private float hitstopDuration = 0.06f;

        private Coroutine _autoCloseCoroutine;

        public override void WeaponHitDamageable()
        {
            base.WeaponHitDamageable();
            HitstopTrigger.Trigger(hitstopDuration);
        }

        public bool DamageWindowOpen =>
            _damageAreaCollider != null && _damageAreaCollider.enabled;

        protected override IEnumerator MeleeWeaponAttack()
        {
            if (!useAnimationEventDamageWindow)
            {
                yield return StartCoroutine(base.MeleeWeaponAttack());
                yield break;
            }

            if (_attackInProgress)
            {
                yield break;
            }

            DisableDamageArea();
            yield break;
        }

        public void OpenDamageWindow()
        {
            if (!useAnimationEventDamageWindow)
            {
                return;
            }

            if (_damageAreaCollider == null)
            {
                Debug.LogWarning($"{name}: DamageArea belum siap.", this);
                return;
            }

            _attackInProgress = true;
            EnableDamageArea();

            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
            }

            if (autoCloseAfterOpen)
            {
                _autoCloseCoroutine = StartCoroutine(AutoCloseDamageWindow());
            }

            if (logDamageWindow)
            {
                Debug.Log($"{name}: damage window OPEN.", this);
            }
        }

        public void CloseDamageWindow()
        {
            if (_autoCloseCoroutine != null)
            {
                StopCoroutine(_autoCloseCoroutine);
                _autoCloseCoroutine = null;
            }

            if (_damageAreaCollider != null)
            {
                DisableDamageArea();
            }

            _attackInProgress = false;

            if (logDamageWindow)
            {
                Debug.Log($"{name}: damage window CLOSED.", this);
            }
        }

        private IEnumerator AutoCloseDamageWindow()
        {
            yield return new WaitForSeconds(damageWindowDuration);
            _autoCloseCoroutine = null;
            CloseDamageWindow();
        }

        public override void TurnWeaponOff()
        {
            base.TurnWeaponOff();
            CloseDamageWindow();
        }

        protected override void OnDisable()
        {
            CloseDamageWindow();
            base.OnDisable();
        }
    }
}
