using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[CreateAssetMenu(fileName = "PlaceholderActiveSkill", menuName = "Maskbound/Skills/Placeholder Active Skill")]
	public class PlaceholderActiveSkill : Skill
	{
		public float Cooldown = 1f;
		protected float _lastActivatedAt = -999f;

		protected virtual void OnValidate()
		{
			SkillType = SkillType.Active;
		}

		public override bool CanActivate(SkillContext context)
		{
			return base.CanActivate(context) && Time.time >= _lastActivatedAt + Cooldown;
		}

		public override bool Activate(SkillContext context)
		{
			if (!CanActivate(context))
			{
				return false;
			}

			_lastActivatedAt = Time.time;
			if (context.Owner != null)
			{
				context.Owner.SendMessage("OnSkillActiveActivated", this, SendMessageOptions.DontRequireReceiver);
			}
			Debug.Log($"Active skill activated: {DisplayName} from slot {context.SlotIndex}", context.Owner);
			return true;
		}
	}
}
