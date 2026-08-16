using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    /// <summary>
    /// Single action that picks and performs the right attack for the boss's current phase.
    /// On state entry it reads the boss HP% and the distance to the target, then equips the
    /// matching weapon and triggers the attack. The paired AIDecisionPhaseAttackDone reads
    /// CurrentAttackDuration / ShouldChase to decide when to transition back to Chase.
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

        [Header("Durations")]
        [Tooltip("Randomized duration range for the phase 1 Attack1.")]
        public float Attack1DurationMin = 4f;
        public float Attack1DurationMax = 5f;
        public float Attack2Duration = 4f;
        public float SpecialDuration = 6f;

        /// <summary>
        /// Duration of the attack that was just picked. Read by AIDecisionPhaseAttackDone.
        /// </summary>
        public float CurrentAttackDuration { get; protected set; }

        /// <summary>
        /// True when the action decided the boss should go back to chasing. Read by AIDecisionPhaseAttackDone.
        /// </summary>
        public bool ShouldChase { get; protected set; }

        protected CharacterHandleWeapon _characterHandleWeapon;
        protected Health _health;
        protected bool _useMaskRage;
        protected int _attacksPerformed;

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
            _attacksPerformed = 0;
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
            if (_attacksPerformed >= 1) return;
            _attacksPerformed++;
            PerformPhaseAttack();
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
            float duration = Random.Range(Attack1DurationMin, Attack1DurationMax);

            // Phase 3 (HP <= 50%): no attack 2 at all - mask rage when the player is close,
            // otherwise laser beam / mask rage alternate.
            if (hpPercentage <= Phase3Threshold)
            {
                if (distance <= Attack2Distance)
                {
                    chosenWeapon = MaskRageWeapon;
                    duration = SpecialDuration;
                }
                else
                {
                    chosenWeapon = _useMaskRage ? MaskRageWeapon : LaserBeamWeapon;
                    _useMaskRage = !_useMaskRage;
                    duration = SpecialDuration;
                }
            }
            // Close-range melee (attack 2) is available in phases 1 and 2.
            else if (distance <= Attack2Distance)
            {
                chosenWeapon = Attack2Weapon;
                duration = Attack2Duration;
            }
            // Phase 2: rain hammer replaces attack 1.
            else if (hpPercentage <= Phase2Threshold)
            {
                chosenWeapon = RainHammerWeapon;
                duration = SpecialDuration;
            }

            if (_characterHandleWeapon != null)
            {
                if (chosenWeapon != null)
                {
                    _characterHandleWeapon.ChangeWeapon(chosenWeapon, chosenWeapon.name);
                }
                _characterHandleWeapon.ShootStart();
            }

            CurrentAttackDuration = duration;
        }
    }
}
