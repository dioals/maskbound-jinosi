using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Skill Animation Event Relay")]
	public class SkillAnimationEventRelay : MonoBehaviour
	{
		public CharacterSkillCaster SkillCaster;

		protected virtual void Awake()
		{
			if (SkillCaster == null)
			{
				SkillCaster = GetComponentInParent<CharacterSkillCaster>();
			}

			if (SkillCaster == null)
			{
				SkillCaster = GetComponentInChildren<CharacterSkillCaster>(true);
			}

			if (SkillCaster == null && transform.root != null)
			{
				SkillCaster = transform.root.GetComponentInChildren<CharacterSkillCaster>(true);
			}
		}

		public virtual void StopCastingAnimation()
		{
			if (SkillCaster != null)
			{
				SkillCaster.StopCastingAnimation();
			}
		}

		public virtual void SpawnCurrentSkill()
		{
			SpawnCurrentSkillProjectile();
		}

		public virtual void SpawnCurrentSkillProjectile()
		{
			if (SkillCaster != null)
			{
				SkillCaster.SpawnCurrentSkillProjectile();
			}
		}
	}
}
