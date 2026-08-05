using MoreMountains.CorgiEngine;
using MaskboundJinosi.Skills.Passives;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	public readonly struct SkillRuntimeContext
	{
		public readonly ActiveSkillData Skill;
		public readonly GameObject Owner;
		public readonly Character Character;
		public readonly Health Health;
		public readonly int SlotIndex;
		public readonly bool FacingRight;
		public readonly float Damage;
		public readonly float Duration;

		public SkillRuntimeContext(ActiveSkillData skill, SkillContext context, bool facingRight)
		{
			Skill = skill;
			Owner = context.Owner;
			Character = context.Character;
			Health = context.Health;
			SlotIndex = context.SlotIndex;
			FacingRight = facingRight;
			float baseDamage = skill != null ? skill.Damage : 0f;
			PlayerPassiveSkillController passiveController = Owner != null
				? Owner.GetComponentInParent<PlayerPassiveSkillController>()
				: null;
			Damage = passiveController != null ? passiveController.ModifySkillDamage(baseDamage) : baseDamage;
			Duration = skill != null ? skill.Duration : 0f;
		}
	}
}
