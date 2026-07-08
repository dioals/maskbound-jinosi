using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	public readonly struct SkillContext
	{
		public readonly SkillSlotManager SlotManager;
		public readonly GameObject Owner;
		public readonly Character Character;
		public readonly Health Health;
		public readonly int SlotIndex;

		public SkillContext(SkillSlotManager slotManager, int slotIndex)
		{
			SlotManager = slotManager;
			Owner = slotManager != null ? slotManager.gameObject : null;
			Character = slotManager != null ? slotManager.GetComponentInParent<Character>() : null;
			Health = Character != null ? Character.CharacterHealth : (Owner != null ? Owner.GetComponentInParent<Health>() : null);
			SlotIndex = slotIndex;
		}
	}
}
