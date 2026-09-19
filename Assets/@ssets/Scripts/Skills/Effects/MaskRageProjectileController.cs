using System.Collections;
using System.Collections.Generic;
using MaskboundJinosi.Combat;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
    /// <summary>
    /// Controls the PrabuKlana MaskRage projectile lifecycle.
    ///
    /// Phase 1 (shadow idle): the shadow damage is active from spawn, so touching
    /// the shadow hurts. If the shadow is hit by a player-owned attack (same detection as
    /// BreakableObject) it explodes immediately; otherwise it explodes automatically
    /// after ExplodeDelaySeconds.
    ///
    /// Phase 2 (mask rage): triggers the animator's "Explode" state, disables the
    /// shadow damage and enables the bite DamageOnTouch so the giant mask deals damage.
    /// </summary>
    [AddComponentMenu("Maskbound/Skills/Effects/Mask Rage Projectile Controller")]
    public class MaskRageProjectileController : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds the shadow idle lasts before exploding automatically.")]
        [SerializeField, Min(0f)] private float explodeDelay = 5f;

        [Header("Damage")]
        [Tooltip("Damage saat terkena bayangan BOSS (fase shadow idle). Aktif sejak spawn, mati saat meledak.")]
        [SerializeField] private DamageOnTouch shadowDamage;
        [Tooltip("Damage saat terkena gigitan topeng raksasa (fase explode). Aktif sebentar setelah meledak.")]
        [SerializeField] private DamageOnTouch biteDamage;
        [Tooltip("Optional relay that enables the bite DamageOnTouch. Found automatically if empty.")]
        [SerializeField] private DamageOnTouchAnimationRelay biteDamageRelay;
        [Tooltip("Enables the bite damage right when the explode trigger fires. If false, keep the bite DamageOnTouch disabled and enable it via an Animation Event calling EnableDamage().")]
        [SerializeField] private bool enableDamageOnExplode = true;
        [Tooltip("How long the bite damage stays active after exploding (a quick damage window). 0 = stays active forever until the object dies.")]
        [SerializeField, Min(0f)] private float damageWindowDuration = 0.3f;

        [Header("Hit Detection")]
        [Tooltip("When the shadow is hit by a player attack, it explodes immediately.")]
        [SerializeField] private bool explodeOnHit = true;

        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private Animator _animator;
        private Coroutine _explodeCoroutine;
        private bool _exploded;

        protected virtual void Awake()
        {
            ResolveReferences();

            // Fase shadow idle: damage bayangan aktif sejak spawn, damage gigitan mati.
            SetDamageEnabled(shadowDamage, true);
            SetBiteDamageEnabled(false);
        }

        protected virtual void OnEnable()
        {
            if (_exploded)
            {
                return;
            }

            StartExplodeTimer();
        }

        protected virtual void OnDisable()
        {
            StopExplodeTimer();
        }

        protected virtual void OnDestroy()
        {
            StopExplodeTimer();
        }

        /// <summary>
        /// Force the projectile to explode now (used when the shadow gets hit).
        /// </summary>
        public void TriggerNow()
        {
            if (_exploded)
            {
                return;
            }

            if (logEvents)
            {
                Debug.Log($"{name}: MaskRage triggered now.", this);
            }

            Explode();
        }

        /// <summary>
        /// Explode immediately, ignoring any pending timer.
        /// </summary>
        protected virtual void Explode()
        {
            if (_exploded)
            {
                return;
            }

            _exploded = true;
            StopExplodeTimer();

            if (_animator != null)
            {
                // The shadow's animator uses a boolean "Explode" parameter
                // (Idle -> MaskRage transition), not a trigger.
                _animator.SetBool("Explode", true);
            }

            if (enableDamageOnExplode)
            {
                // Bayangan sudah meledak: matikan damage bayangan, nyalakan damage gigitan sebentar.
                SetDamageEnabled(shadowDamage, false);
                SetBiteDamageEnabled(true);

                // Quick damage window: disable the collider again after a short time.
                if (damageWindowDuration > 0f)
                {
                    StartCoroutine(DisableDamageAfterWindowCo());
                }
            }
        }

        private IEnumerator DisableDamageAfterWindowCo()
        {
            if (logEvents)
            {
                Debug.Log($"{name}: Damage window active for {damageWindowDuration}s.", this);
            }

            yield return new WaitForSeconds(damageWindowDuration);

            if (logEvents)
            {
                Debug.Log($"{name}: Damage window over, disabling damage.", this);
            }

            SetBiteDamageEnabled(false);
        }

        private void SetBiteDamageEnabled(bool enabled)
        {
            ResolveDamageRelay();
            if (biteDamageRelay != null)
            {
                biteDamageRelay.SetDamageEnabled(enabled);
            }
            else
            {
                SetDamageEnabled(biteDamage, enabled);
            }
        }

        /// <summary>
        /// Starts (or restarts) the countdown to the automatic explosion.
        /// </summary>
        protected virtual void StartExplodeTimer()
        {
            StopExplodeTimer();

            if (explodeDelay <= 0f)
            {
                return;
            }

            _explodeCoroutine = StartCoroutine(ExplodeTimerCo());
        }

        protected virtual void StopExplodeTimer()
        {
            if (_explodeCoroutine != null)
            {
                StopCoroutine(_explodeCoroutine);
                _explodeCoroutine = null;
            }
        }

        private IEnumerator ExplodeTimerCo()
        {
            if (logEvents)
            {
                Debug.Log($"{name}: Shadow idle - exploding in {explodeDelay}s.", this);
            }

            yield return new WaitForSeconds(explodeDelay);

            if (logEvents)
            {
                Debug.Log($"{name}: Shadow idle timer elapsed, exploding.", this);
            }

            Explode();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryExplodeFromPlayerHit(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryExplodeFromPlayerHit(other);
        }

        private void TryExplodeFromPlayerHit(Collider2D other)
        {
            if (_exploded || !explodeOnHit || other == null)
            {
                return;
            }

            // Touched directly by the player character.
            if (other.CompareTag("Player"))
            {
                if (logEvents)
                {
                    Debug.Log($"{name}: Shadow touched by player, exploding.", this);
                }

                TriggerNow();
                return;
            }

            // Hit by a player-owned attack (weapon/projectile/skill hitbox).
            if (IsPlayerOwnedDamage(other))
            {
                if (logEvents)
                {
                    Debug.Log($"{name}: Shadow hit by player attack, exploding.", this);
                }

                TriggerNow();
            }
        }

        private bool IsPlayerOwnedDamage(Collider2D sourceCollider)
        {
            DamageOnTouch damageOnTouch = sourceCollider.GetComponent<DamageOnTouch>();
            if (damageOnTouch == null)
            {
                damageOnTouch = sourceCollider.GetComponentInParent<DamageOnTouch>();
            }

            if (damageOnTouch == null)
            {
                return false;
            }

            GameObject owner = damageOnTouch.Owner;
            if (owner == null)
            {
                Weapon weapon = sourceCollider.GetComponentInParent<Weapon>();
                if (weapon != null && weapon.Owner != null)
                {
                    owner = weapon.Owner.gameObject;
                }
            }

            if (owner == null)
            {
                return false;
            }

            Character ownerCharacter = owner.GetComponentInParent<Character>();
            return ownerCharacter != null && ownerCharacter.CharacterType == Character.CharacterTypes.Player;
        }

        private void SetDamageEnabled(DamageOnTouch damage, bool enabled)
        {
            if (damage != null)
            {
                damage.enabled = enabled;
            }
        }

        private void ResolveDamageRelay()
        {
            ResolveDamageTargets();

            if (biteDamageRelay == null)
            {
                biteDamageRelay = GetComponentInChildren<DamageOnTouchAnimationRelay>(true);
            }
        }

        private void ResolveDamageTargets()
        {
            if (shadowDamage != null && biteDamage != null)
            {
                return;
            }

            DamageOnTouch[] damages = GetComponentsInChildren<DamageOnTouch>(true);
            for (int i = 0; i < damages.Length; i++)
            {
                if (damages[i] == null)
                {
                    continue;
                }

                if (shadowDamage == null && damages[i].name.ToLowerInvariant().Contains("shadow"))
                {
                    shadowDamage = damages[i];
                }
                else if (biteDamage == null && damages[i] != shadowDamage)
                {
                    biteDamage = damages[i];
                }
            }
        }

        private void ResolveReferences()
        {
            _animator = GetComponentInChildren<Animator>(true);
            ResolveDamageRelay();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }
    }
}
