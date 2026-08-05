using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Skills.Passives
{
    [AddComponentMenu("Maskbound/Skills/Player Passive Skill Controller")]
    public class PlayerPassiveSkillController : MonoBehaviour, MMEventListener<MMDamageTakenEvent>, IOutgoingDamageModifier
    {
        private readonly Dictionary<PassiveSkillData, List<PassiveEffectRuntime>> _skillRuntimes =
            new Dictionary<PassiveSkillData, List<PassiveEffectRuntime>>();
        private readonly List<PassiveEffectRuntime> _allRuntimes = new List<PassiveEffectRuntime>();

        private Character _character;
        private CharacterHorizontalMovement _movement;
        private float _baseMovementMultiplier = 1f;

        public Health PlayerHealth { get; private set; }
        public float SkillDamageMultiplier { get; private set; } = 1f;
        public float SkillCooldownMultiplier { get; private set; } = 1f;
        public float MovementSpeedMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            ResolveReferences();
            RecalculateModifiers();
        }

        private void OnEnable()
        {
            this.MMEventStartListening<MMDamageTakenEvent>();
        }

        private void OnDisable()
        {
            this.MMEventStopListening<MMDamageTakenEvent>();
        }

        private void Update()
        {
            for (int i = 0; i < _allRuntimes.Count; i++)
            {
                _allRuntimes[i].Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            foreach (PassiveEffectRuntime runtime in _allRuntimes)
            {
                runtime.Dispose();
            }

            RestoreMovementMultiplier();
        }

        public void Register(PassiveSkillData skill)
        {
            if (skill == null || _skillRuntimes.ContainsKey(skill))
            {
                return;
            }

            List<PassiveEffectRuntime> runtimes = new List<PassiveEffectRuntime>();
            if (skill.Effects != null)
            {
                foreach (PassiveEffectData effect in skill.Effects)
                {
                    if (effect == null)
                    {
                        continue;
                    }

                    PassiveEffectRuntime runtime = effect.CreateRuntime(this);
                    if (runtime != null)
                    {
                        runtimes.Add(runtime);
                        _allRuntimes.Add(runtime);
                    }
                }
            }

            _skillRuntimes.Add(skill, runtimes);
            RecalculateModifiers();
        }

        public void Unregister(PassiveSkillData skill)
        {
            if (skill == null || !_skillRuntimes.TryGetValue(skill, out List<PassiveEffectRuntime> runtimes))
            {
                return;
            }

            foreach (PassiveEffectRuntime runtime in runtimes)
            {
                runtime.Dispose();
                _allRuntimes.Remove(runtime);
            }

            _skillRuntimes.Remove(skill);
            RecalculateModifiers();
        }

        public float ModifySkillDamage(float baseDamage)
        {
            return Mathf.Max(0f, baseDamage * SkillDamageMultiplier);
        }

        public float ModifySkillCooldown(float baseCooldown)
        {
            return Mathf.Max(0f, baseCooldown * SkillCooldownMultiplier);
        }

        public float ModifyOutgoingDamage(Health target, float damage)
        {
            float result = damage;
            for (int i = 0; i < _allRuntimes.Count; i++)
            {
                result = _allRuntimes[i].ModifyOutgoingDamage(target, result);
            }

            return Mathf.Max(0f, result);
        }

        public void OnMMEvent(MMDamageTakenEvent damageEvent)
        {
            if (!IsOwnedInstigator(damageEvent.Instigator) || damageEvent.DamageCaused <= 0f)
            {
                return;
            }

            for (int i = 0; i < _allRuntimes.Count; i++)
            {
                _allRuntimes[i].OnDamageDealt(damageEvent);
            }
        }

        private bool IsOwnedInstigator(GameObject instigator)
        {
            if (instigator == null)
            {
                return false;
            }

            ResolveReferences();
            Character instigatorCharacter = instigator.GetComponentInParent<Character>();
            return instigatorCharacter != null && instigatorCharacter == _character;
        }

        private void RecalculateModifiers()
        {
            SkillDamageMultiplier = 1f;
            SkillCooldownMultiplier = 1f;
            MovementSpeedMultiplier = 1f;

            foreach (PassiveEffectRuntime runtime in _allRuntimes)
            {
                SkillDamageMultiplier *= runtime.SkillDamageMultiplier;
                SkillCooldownMultiplier *= runtime.SkillCooldownMultiplier;
                MovementSpeedMultiplier *= runtime.MovementSpeedMultiplier;
            }

            ApplyMovementMultiplier();
        }

        private void ResolveReferences()
        {
            if (_character == null)
            {
                _character = GetComponentInParent<Character>();
            }

            if (PlayerHealth == null && _character != null)
            {
                PlayerHealth = _character.CharacterHealth;
            }

            if (_movement == null && _character != null)
            {
                _movement = _character.FindAbility<CharacterHorizontalMovement>();
                if (_movement != null)
                {
                    _baseMovementMultiplier = _movement.MovementSpeedMultiplier;
                }
            }
        }

        private void ApplyMovementMultiplier()
        {
            ResolveReferences();
            if (_movement != null)
            {
                _movement.MovementSpeedMultiplier = _baseMovementMultiplier * MovementSpeedMultiplier;
            }
        }

        private void RestoreMovementMultiplier()
        {
            if (_movement != null)
            {
                _movement.MovementSpeedMultiplier = _baseMovementMultiplier;
            }
        }
    }
}
