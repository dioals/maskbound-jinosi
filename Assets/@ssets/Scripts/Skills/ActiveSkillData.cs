using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[CreateAssetMenu(fileName = "ActiveSkillData", menuName = "Maskbound/Skills/Active Skill Data")]
	public class ActiveSkillData : Skill
	{
		[Header("Cast")]
		[Min(0f)] public float Damage;
		[Min(0f)] public float Duration;
		[Min(0f)] public float Cooldown = 1f;
		public int SkillIndex;
		public string CastTriggerParameter = "SkillCast";
		public string SkillIndexParameter = "SkillIndex";
		public string IsCastingParameter = "IsCastingSkill";

		[Header("Spawn")]
		public GameObject SkillPrefab;
		public Vector2 SpawnOffset = new Vector2(1f, 0f);
		public bool ParentToOwner;
		public bool MatchOwnerFacing = true;

		[Header("Projectile")]
		public GameObject ProjectilePrefab;
		public Vector2 ProjectileSpawnOffset = new Vector2(1f, 0f);
		public bool ProjectileParentToOwner;
		public bool ProjectileMatchOwnerFacing = true;

		public override bool CanActivate(SkillContext context)
		{
			if (!base.CanActivate(context))
			{
				return false;
			}

			CharacterSkillCaster caster = GetCaster(context);
			return caster != null && caster.CanCast(this);
		}

		public override bool Activate(SkillContext context)
		{
			CharacterSkillCaster caster = GetCaster(context);
			return caster != null && caster.Cast(this, context);
		}

		protected virtual CharacterSkillCaster GetCaster(SkillContext context)
		{
			if (context.Owner == null)
			{
				return null;
			}

			return context.Owner.GetComponentInChildren<CharacterSkillCaster>(true);
		}

		protected virtual void OnValidate()
		{
			SkillType = SkillType.Active;
		}
	}
}
