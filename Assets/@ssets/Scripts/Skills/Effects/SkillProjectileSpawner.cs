using MaskboundJinosi.Skills;
using MoreMountains.CorgiEngine;
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
		[Tooltip("Aktifkan jika visual/collider prefab mengarah ke kanan pada scale X positif. Matikan jika prefab default mengarah ke kiri.")]
		public bool PrefabFacesRight = true;
		[Tooltip("Mencegah Animation Event yang terpanggil berulang membuat lebih dari satu projectile.")]
		public bool SpawnOnlyOnce = true;
		public bool LogDebug;

		protected SkillRuntimeContext _context;
		protected bool _hasContext;
		protected bool _hasSpawned;
		protected Character _ownerCharacter;

		protected virtual void Awake()
		{
			_ownerCharacter = GetComponentInParent<Character>();
		}

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

			bool facingRight = ResolveFacingRight();
			Vector2 offset = SpawnOffset;
			if (MatchOwnerFacing)
			{
				offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);
			}

			Transform origin = SpawnOrigin != null ? SpawnOrigin : transform;
			Transform parent = ParentProjectileToOwner ? origin : null;
			GameObject instance = Instantiate(prefab, origin.position + (Vector3)offset, Quaternion.identity, parent);
			_hasSpawned = true;

			if (MatchOwnerFacing && facingRight != PrefabFacesRight)
			{
				Vector3 scale = instance.transform.localScale;
				scale.x *= -1f;
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

		protected virtual bool ResolveFacingRight()
		{
			if (_hasContext)
			{
				return _context.FacingRight;
			}

			if (_ownerCharacter == null)
			{
				_ownerCharacter = GetComponentInParent<Character>();
			}

			return _ownerCharacter == null || _ownerCharacter.IsFacingRight;
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
