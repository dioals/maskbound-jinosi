using MaskboundJinosi.Skills;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Skill Projectile Spawner")]
	public class SkillProjectileSpawner : MonoBehaviour, ISkillRuntimeReceiver
	{
		public GameObject ProjectilePrefab;
		public Transform SpawnOrigin;
		public Vector2 SpawnOffset = Vector2.right;
		public bool UseSkillDataProjectile = true;
		public bool ParentProjectileToOwner;
		public bool MatchOwnerFacing = true;
		[Tooltip("Mencegah Animation Event yang terpanggil berulang membuat lebih dari satu projectile.")]
		public bool SpawnOnlyOnce = true;
		public bool LogDebug;

		protected SkillRuntimeContext _context;
		protected bool _hasContext;
		protected bool _hasSpawned;

		public virtual void Initialize(SkillRuntimeContext context)
		{
			_context = context;
			_hasContext = true;
			_hasSpawned = false;
		}

		public virtual void SpawnProjectile()
		{
			if (SpawnOnlyOnce && _hasSpawned)
			{
				return;
			}

			GameObject prefab = ResolveProjectilePrefab();
			if (prefab == null)
			{
				if (LogDebug)
				{
					Debug.LogWarning("SpawnProjectile was called, but no projectile prefab is assigned.", this);
				}

				return;
			}

			bool facingRight = !_hasContext || _context.FacingRight;
			Vector2 offset = SpawnOffset;
			if (MatchOwnerFacing)
			{
				offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);
			}

			Transform origin = SpawnOrigin != null ? SpawnOrigin : transform;
			Transform parent = ParentProjectileToOwner ? origin : null;
			GameObject instance = Instantiate(prefab, origin.position + (Vector3)offset, Quaternion.identity, parent);
			_hasSpawned = true;

			if (MatchOwnerFacing && !facingRight)
			{
				Vector3 scale = instance.transform.localScale;
				scale.x = -Mathf.Abs(scale.x);
				instance.transform.localScale = scale;
			}

			if (_hasContext)
			{
				ISkillRuntimeReceiver[] receivers = instance.GetComponentsInChildren<ISkillRuntimeReceiver>(true);
				for (int i = 0; i < receivers.Length; i++)
				{
					receivers[i].Initialize(_context);
				}
			}
		}

		protected virtual GameObject ResolveProjectilePrefab()
		{
			if (UseSkillDataProjectile && _hasContext && _context.Skill != null && _context.Skill.ProjectilePrefab != null)
			{
				return _context.Skill.ProjectilePrefab;
			}

			return ProjectilePrefab;
		}
	}
}
