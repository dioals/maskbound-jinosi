using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Passives
{
    public abstract class PassiveEffectData : ScriptableObject
    {
        public abstract PassiveEffectRuntime CreateRuntime(PlayerPassiveSkillController controller);
    }

    public abstract class PassiveEffectRuntime
    {
        protected readonly PlayerPassiveSkillController Controller;

        protected PassiveEffectRuntime(PlayerPassiveSkillController controller)
        {
            Controller = controller;
        }

        public virtual float SkillDamageMultiplier => 1f;
        public virtual float SkillCooldownMultiplier => 1f;
        public virtual float MovementSpeedMultiplier => 1f;
        public virtual float ModifyOutgoingDamage(Health target, float damage) => damage;
        public virtual void OnDamageDealt(MMDamageTakenEvent damageEvent) { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Dispose() { }
    }
}
