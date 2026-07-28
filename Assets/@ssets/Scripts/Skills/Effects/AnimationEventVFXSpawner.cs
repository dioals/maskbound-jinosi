using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Animation Event VFX Spawner")]
	public class AnimationEventVFXSpawner : MonoBehaviour
	{
		[Header("Spawn")]
		public GameObject VFXPrefab;
		public Transform SpawnOrigin;
		public Vector2 SpawnOffset;
		public bool ParentToOrigin;
		public bool MatchOwnerFacing = true;

		[Header("Animator")]
		public string VFXAnimatorIntParameter = "IndexVFX";
		public int VFXAnimationIndex;

		[Header("Lifetime")]
		public bool AutoDestroySpawnedVFX = true;
		[Min(0f)] public float DestroyDelay = 1f;

		[Header("Debug")]
		public bool LogDebug;

		protected Character _character;

		protected virtual void Awake()
		{
			_character = GetComponentInParent<Character>();

			if (SpawnOrigin == null)
			{
				SpawnOrigin = transform;
			}
		}

		public virtual void SpawnVFX()
		{
			SpawnVFXWithIndex(VFXAnimationIndex);
		}

		public virtual void SpawnVFXWithIndex(int animationIndex)
		{
			if (VFXPrefab == null)
			{
				if (LogDebug)
				{
					Debug.LogWarning("SpawnVFX was called, but no VFX prefab is assigned.", this);
				}

				return;
			}

			bool facingRight = ResolveFacingRight();
			Vector2 offset = SpawnOffset;
			if (MatchOwnerFacing)
			{
				offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);
			}

			Transform parent = ParentToOrigin ? SpawnOrigin : null;
			GameObject instance = Instantiate(VFXPrefab, SpawnOrigin.position + (Vector3)offset, Quaternion.identity, parent);
			ApplyAnimatorIndex(instance, animationIndex);

			if (MatchOwnerFacing && !facingRight)
			{
				Vector3 scale = instance.transform.localScale;
				scale.x = -Mathf.Abs(scale.x);
				instance.transform.localScale = scale;
			}

			if (AutoDestroySpawnedVFX)
			{
				Destroy(instance, DestroyDelay);
			}
		}

		public virtual void SpawnVFXIndex1()
		{
			SpawnVFXWithIndex(1);
		}

		public virtual void SpawnVFXIndex2()
		{
			SpawnVFXWithIndex(2);
		}

		public virtual void SpawnVFXIndex3()
		{
			SpawnVFXWithIndex(3);
		}

		protected virtual void ApplyAnimatorIndex(GameObject instance, int animationIndex)
		{
			if (instance == null || string.IsNullOrEmpty(VFXAnimatorIntParameter))
			{
				return;
			}

			Animator animator = instance.GetComponentInChildren<Animator>(true);
			if (animator == null)
			{
				return;
			}

			animator.SetInteger(VFXAnimatorIntParameter, animationIndex);
		}

		protected virtual bool ResolveFacingRight()
		{
			if (_character != null)
			{
				return _character.IsFacingRight;
			}

			return transform.lossyScale.x >= 0f;
		}
	}
}
