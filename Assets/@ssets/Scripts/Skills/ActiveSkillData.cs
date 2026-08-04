using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[CreateAssetMenu(fileName = "ActiveSkillData", menuName = "Maskbound/Skills/Active Skill Data")]
	public class ActiveSkillData : Skill
	{
		[Header("Cast")]
		[Min(0f)] public float Damage;
		[Min(0f)] public float Duration;
		[Tooltip("Fallback: how long the player is locked out of jumping/attacking/blocking/re-casting if the cast animation never fires its StopCastingAnimation event. Should match the cast animation's length, not the summoned effect's Duration above.")]
		[Min(0f)] public float CastLockFallbackDuration = 1f;
		[Min(0f)] public float Cooldown = 1f;
		public int SkillIndex;
		public string CastTriggerParameter = "SkillCast";
		public string SkillIndexParameter = "SkillIndex";
		public string IsCastingParameter = "IsCastingSkill";

		[Header("Spawn")]
		public GameObject SkillPrefab;
		[Tooltip("Delay setelah animasi cast dimulai sebelum SkillPrefab dibuat. Atur per skill agar efek muncul tepat pada frame animasi yang diinginkan.")]
		[Min(0f)] public float SkillPrefabSpawnDelay;
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
