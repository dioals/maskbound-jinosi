using UnityEngine;

namespace MaskboundJinosi.Skills.Passives
{
    [CreateAssetMenu(fileName = "StatModifierEffect", menuName = "Maskbound/Skills/Passive Effects/Stat Modifier")]
    public class StatModifierPassiveEffect : PassiveEffectData
    {
        public enum ModifiedStats { SkillDamage, SkillCooldown, MovementSpeed }

        public ModifiedStats Stat;
        [Tooltip("1.10 berarti +10%. 0.70 berarti -30%.")]
        [Min(0f)] public float Multiplier = 1f;

        public override PassiveEffectRuntime CreateRuntime(PlayerPassiveSkillController controller)
        {
            return new Runtime(controller, this);
        }

        private sealed class Runtime : PassiveEffectRuntime
        {
            private readonly StatModifierPassiveEffect _data;

            public Runtime(PlayerPassiveSkillController controller, StatModifierPassiveEffect data) : base(controller)
            {
                _data = data;
            }

            public override float SkillDamageMultiplier => _data.Stat == ModifiedStats.SkillDamage ? _data.Multiplier : 1f;
            public override float SkillCooldownMultiplier => _data.Stat == ModifiedStats.SkillCooldown ? _data.Multiplier : 1f;
            public override float MovementSpeedMultiplier => _data.Stat == ModifiedStats.MovementSpeed ? _data.Multiplier : 1f;
        }
    }
}
