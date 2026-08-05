using System.Collections.Generic;
using UnityEngine;

namespace MaskboundJinosi.Skills.Passives
{
    [CreateAssetMenu(fileName = "PassiveSkill", menuName = "Maskbound/Skills/Passive Skill Data")]
    public class PassiveSkillData : Skill
    {
        [Header("Effects")]
        public List<PassiveEffectData> Effects = new List<PassiveEffectData>();

        public override bool CanActivate(SkillContext context) => false;

        public override void OnEquipped(SkillContext context)
        {
            base.OnEquipped(context);
            context.Owner?.GetComponentInParent<PlayerPassiveSkillController>()?.Register(this);
        }

        public override void OnUnequipped(SkillContext context)
        {
            context.Owner?.GetComponentInParent<PlayerPassiveSkillController>()?.Unregister(this);
            base.OnUnequipped(context);
        }

        protected virtual void OnValidate()
        {
            SkillType = SkillType.Passive;
        }
    }
}
