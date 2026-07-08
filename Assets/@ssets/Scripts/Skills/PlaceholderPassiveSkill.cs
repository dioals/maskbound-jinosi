using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[CreateAssetMenu(fileName = "PlaceholderPassiveSkill", menuName = "Maskbound/Skills/Placeholder Passive Skill")]
	public class PlaceholderPassiveSkill : Skill
	{
		public string EquippedFlagName = "PlaceholderPassiveEquipped";

		protected virtual void OnValidate()
		{
			SkillType = SkillType.Passive;
		}

		public override void OnEquipped(SkillContext context)
		{
			base.OnEquipped(context);
			if (context.Owner != null)
			{
				context.Owner.SendMessage("OnSkillPassiveEquipped", this, SendMessageOptions.DontRequireReceiver);
			}
			Debug.Log($"Passive skill equipped: {DisplayName} on slot {context.SlotIndex}", context.Owner);
		}

		public override void OnUnequipped(SkillContext context)
		{
			base.OnUnequipped(context);
			if (context.Owner != null)
			{
				context.Owner.SendMessage("OnSkillPassiveUnequipped", this, SendMessageOptions.DontRequireReceiver);
			}
			Debug.Log($"Passive skill unequipped: {DisplayName} from slot {context.SlotIndex}", context.Owner);
		}
	}
}
