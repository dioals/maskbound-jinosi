using MoreMountains.CorgiEngine;
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
			Damage = skill != null ? skill.Damage : 0f;
			Duration = skill != null ? skill.Duration : 0f;
		}
	}
}
