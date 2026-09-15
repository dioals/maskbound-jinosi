using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    /// <summary>
    /// Single action that picks and performs the right attack for the boss's current phase.
    /// On state entry it reads the boss HP% and the distance to the target, then equips the
    /// matching weapon and triggers the attack. It keeps chaining attacks (re-evaluating phase
    /// and distance every time) until AttacksPerState is reached, so the boss doesn't stand
    /// idle waiting for a fixed timer to run out.
    /// The paired AIDecisionPhaseAttackDone reads SequenceComplete / ShouldChase to decide when
    /// to transition back to Chase.
    /// </summary>
    [AddComponentMenu("Maskbound/AI/Actions/AI Action Phase Attack")]
    public class AIActionPhaseAttack : AIAction
    {
        [Header("Phase Thresholds (HP fraction, 0-1)")]
        [Tooltip("Below this HP fraction phase 2 (rain hammer) starts.")]
        [Range(0f, 1f)] public float Phase2Threshold = 0.7f;
        [Tooltip("Below this HP fraction phase 3 (laser beam / mask rage) starts.")]
        [Range(0f, 1f)] public float Phase3Threshold = 0.5f;

        [Header("Distances")]
        [Tooltip("Max distance for the close-range melee (Attack2). Used in every phase.")]
        public float Attack2Distance = 14.5f;
        [Tooltip("Max distance for the main attack (Attack1 or phase special). Beyond this the boss goes back to chasing.")]
        public float AttackDistance = 16f;

        [Header("Weapons")]
        public Weapon Attack1Weapon;
        public Weapon Attack2Weapon;
        public Weapon RainHammerWeapon;
        public Weapon LaserBeamWeapon;
        public Weapon MaskRageWeapon;

        [Header("Aggression")]
        [Tooltip("How many attacks the boss chains before handing control back to the brain. Higher = more relentless.")]
        [Min(1)] public int AttacksPerState = 2;
        [Tooltip("Pause after a melee attack (Attack1 / Attack2) finishes, before the next one can start.")]
        [Min(0f)] public float MeleeRecovery = 0.35f;
        [Tooltip("Pause after a special (rain hammer / laser beam / mask rage) finishes, before the next attack can start.")]
        [Min(0f)] public float SpecialRecovery = 0.6f;

        [Header("Safety Timeouts")]
        [Tooltip("Fallback duration range for Attack1, used only if the weapon never reports back to idle.")]
        public float Attack1DurationMin = 3f;
        public float Attack1DurationMax = 4f;
        [Tooltip("Fallback duration for Attack2, used only if the weapon never reports back to idle.")]
        public float Attack2Duration = 3f;
        [Tooltip("Fallback duration for the phase specials, used only if the weapon never reports back to idle.")]
        public float SpecialDuration = 5f;

        /// <summary>
        /// Safety timeout of the attack that was just picked. Read by AIDecisionPhaseAttackDone
        /// as a fallback in case the weapon never returns to its idle state.
        /// </summary>
        public float CurrentAttackDuration { get; protected set; }

        /// <summary>
        /// True when the action decided the boss should go back to chasing. Read by AIDecisionPhaseAttackDone.
        /// </summary>
        public bool ShouldChase { get; protected set; }

        /// <summary>
        /// True once the boss has performed AttacksPerState attacks and the last one is done.
        /// Read by AIDecisionPhaseAttackDone.
        /// </summary>
        public bool SequenceComplete { get; protected set; }

        protected CharacterHandleWeapon _characterHandleWeapon;
        protected Health _health;
        protected bool _useMaskRage;
        protected int _attacksPerformed;
        protected float _pendingRecovery;
        protected float _recoveryEndsAt;
        protected float _attackStartedAt;
        protected bool _attackInFlight;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            Character character = GetComponentInParent<Character>();
            _characterHandleWeapon = character != null ? character.FindAbility<CharacterHandleWeapon>() : null;
            // Health is not a CharacterAbility in this Corgi version, so we grab it directly
            // (same as AIDecisionHealth does).
            _health = GetComponentInParent<Health>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            CurrentAttackDuration = 0f;
            ShouldChase = false;
            SequenceComplete = false;
            _attacksPerformed = 0;
            _pendingRecovery = 0f;
            _recoveryEndsAt = 0f;
            _attackInFlight = false;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            if (_characterHandleWeapon != null)
            {
                _characterHandleWeapon.ForceStop();
            }
        }

        public override void PerformAction()
        {
            if (ShouldChase || SequenceComplete)
            {
                return;
            }

            if (_attackInFlight)
            {
                // Wait for the weapon to run its full use + recovery cycle. The timeout keeps the
                // boss from locking up if the weapon is destroyed or interrupted mid-attack.
                bool timedOut = (CurrentAttackDuration > 0f)
                                && (Time.time - _attackStartedAt >= CurrentAttackDuration);

                if (WeaponBusy() && !timedOut)
                {
                    return;
                }

                _attackInFlight = false;
                _recoveryEndsAt = Time.time + _pendingRecovery;
            }

            if (Time.time < _recoveryEndsAt)
            {
                return;
            }

            if (_attacksPerformed >= AttacksPerState)
            {
                SequenceComplete = true;
                return;
            }

            _attacksPerformed++;
            PerformPhaseAttack();
        }

        /// <summary>
        /// True while the equipped weapon is still running its start / use / recovery cycle.
        /// </summary>
        protected virtual bool WeaponBusy()
        {
            Weapon weapon = _characterHandleWeapon != null ? _characterHandleWeapon.CurrentWeapon : null;
            if (weapon == null || weapon.WeaponState == null)
            {
                return false;
            }

            return weapon.WeaponState.CurrentState != Weapon.WeaponStates.WeaponIdle;
        }

        protected virtual void PerformPhaseAttack()
        {
            ShouldChase = false;

            float hpPercentage = 1f;
            if (_health != null && _health.MaximumHealth > 0f)
            {
                hpPercentage = _health.CurrentHealth / _health.MaximumHealth;
            }

            float distance = float.MaxValue;
            if (_brain != null && _brain.Target != null)
            {
                distance = Vector2.Distance(this.transform.position, _brain.Target.position);
            }

            // Player too far: tell the brain to go back to chasing.
            if (distance > AttackDistance)
            {
                ShouldChase = true;
                return;
            }

            Weapon chosenWeapon = Attack1Weapon;
            float timeout = Random.Range(Attack1DurationMin, Attack1DurationMax);
            float recovery = MeleeRecovery;

            // Phase 3 (HP <= 50%): no attack 2 at all - mask rage when the player is close,
            // otherwise laser beam / mask rage alternate.
            if (hpPercentage <= Phase3Threshold)
            {
                if (distance <= Attack2Distance)
                {
                    chosenWeapon = MaskRageWeapon;
                }
                else
                {
                    chosenWeapon = _useMaskRage ? MaskRageWeapon : LaserBeamWeapon;
                    _useMaskRage = !_useMaskRage;
                }
                timeout = SpecialDuration;
                recovery = SpecialRecovery;
            }
            // Close-range melee (attack 2) is available in phases 1 and 2.
            else if (distance <= Attack2Distance)
            {
                chosenWeapon = Attack2Weapon;
                timeout = Attack2Duration;
                recovery = MeleeRecovery;
            }
            // Phase 2: rain hammer replaces attack 1.
            else if (hpPercentage <= Phase2Threshold)
            {
                chosenWeapon = RainHammerWeapon;
                timeout = SpecialDuration;
                recovery = SpecialRecovery;
            }

            if (_characterHandleWeapon != null)
            {
                if (chosenWeapon != null && !WeaponAlreadyEquipped(chosenWeapon))
                {
                    // ChangeWeapon destroys and re-instantiates the weapon, which resets its
                    // animator parameters mid-swing, so only do it on an actual weapon change.
                    _characterHandleWeapon.ChangeWeapon(chosenWeapon, chosenWeapon.name);
                }
                _characterHandleWeapon.ShootStart();
            }

            CurrentAttackDuration = timeout;
            // Corgi refuses to re-fire a weapon within its TimeBetweenUses window, so the
            // recovery always needs to be a little above zero or the next swing is swallowed.
            _pendingRecovery = Mathf.Max(recovery, 0.05f);
            _attackStartedAt = Time.time;
            _attackInFlight = true;
        }

        protected virtual bool WeaponAlreadyEquipped(Weapon weapon)
        {
            Weapon current = _characterHandleWeapon != null ? _characterHandleWeapon.CurrentWeapon : null;
            return (current != null) && (current.WeaponID == weapon.name);
        }
    }
}
