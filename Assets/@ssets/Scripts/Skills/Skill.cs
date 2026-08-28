using UnityEngine;

namespace MaskboundJinosi.Skills
{
	public abstract class Skill : ScriptableObject
	{
		[Header("Identity")]
		public string SkillId;
		public string DisplayName;
		[TextArea] public string Description;
		public Sprite Icon;

		[Header("Type")]
		public SkillType SkillType = SkillType.Passive;

		[Header("Shop")]
		[Tooltip("Harga dalam soul untuk membeli skill ini di shop.")]
		[Min(0)] public int SoulPrice;

		public virtual bool CanEquip(SkillContext context)
		{
			return true;
		}

		public virtual void OnEquipped(SkillContext context)
		{
		}

		public virtual void OnUnequipped(SkillContext context)
		{
		}

		public virtual bool CanActivate(SkillContext context)
		{
			return SkillType == SkillType.Active;
		}

		public virtual bool Activate(SkillContext context)
		{
			return false;
		}
	}
}
