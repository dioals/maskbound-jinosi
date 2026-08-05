using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Passives
{
    [CreateAssetMenu(fileName = "ConsecutiveHitEffect", menuName = "Maskbound/Skills/Passive Effects/Consecutive Hit")]
    public class ConsecutiveHitPassiveEffect : PassiveEffectData
    {
        [Min(0f)] public float BonusPerStack = 0.10f;
        [Min(1)] public int MaximumStacks = 5;
        [Min(0f)] public float ResetAfterSeconds = 3f;

        public override PassiveEffectRuntime CreateRuntime(PlayerPassiveSkillController controller)
        {
            return new Runtime(controller, this);
        }

        private sealed class Runtime : PassiveEffectRuntime
        {
            private readonly ConsecutiveHitPassiveEffect _data;
            private Health _target;
            private int _stacks;
            private float _lastHitTime;

            public Runtime(PlayerPassiveSkillController controller, ConsecutiveHitPassiveEffect data) : base(controller)
            {
                _data = data;
            }

            public override float ModifyOutgoingDamage(Health target, float damage)
            {
                ResetExpiredStack();
                if (target == null || target != _target)
                {
                    return damage;
                }

                return damage * (1f + (_stacks * _data.BonusPerStack));
            }

            public override void OnDamageDealt(MMDamageTakenEvent damageEvent)
            {
                Health hitTarget = damageEvent.AffectedHealth;
                if (hitTarget == null)
                {
                    return;
                }

                ResetExpiredStack();
                if (_target != hitTarget)
                {
                    _target = hitTarget;
                    _stacks = 1;
                }
                else
                {
                    _stacks = Mathf.Min(_stacks + 1, Mathf.Max(1, _data.MaximumStacks));
                }

                _lastHitTime = Time.time;
            }

            public override void Tick(float deltaTime)
            {
                ResetExpiredStack();
            }

            private void ResetExpiredStack()
            {
                if (_target == null || (_data.ResetAfterSeconds > 0f && Time.time - _lastHitTime > _data.ResetAfterSeconds))
                {
                    _target = null;
                    _stacks = 0;
                }
            }
        }
    }
}
