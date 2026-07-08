using System;

namespace MaskboundJinosi.Skills
{
	[Serializable]
	public class SkillSlot
	{
		public Skill EquippedSkill;

		public bool IsOccupied => EquippedSkill != null;
	}
}
