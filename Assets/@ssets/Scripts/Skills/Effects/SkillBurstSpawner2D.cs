using System.Collections;
using MaskboundJinosi.Skills;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Skill Burst Spawner 2D")]
	public class SkillBurstSpawner2D : MonoBehaviour, ISkillRuntimeReceiver
	{
		[Header("Spawn")]
		public GameObject SpawnPrefab;
		public Transform SpawnOrigin;
		[Min(1)] public int SpawnCount = 5;
		[Tooltip("Delay sebelum spawn pertama dimulai. Gunakan ini untuk menyamakan ledakan dengan timing animasi pemanggil skill.")]
		[Min(0f)] public float InitialSpawnDelay;
		[Min(0f)] public float DelayBetweenSpawns = 0.2f;
		public Vector2 StartOffset = new Vector2(1f, 0f);
		public Vector2 StepOffset = new Vector2(1f, 0f);
		public bool MatchOwnerFacing = true;
		public bool ParentSpawnedObjectsToOrigin;

		[Header("Lifetime")]
		public bool StartOnInitialize = true;
		public bool DestroyWhenFinished = true;
		[Min(0f)] public float DestroyDelayAfterFinished = 0.25f;

		[Header("Debug")]
		public bool LogDebug;

		protected SkillRuntimeContext _context;
		protected bool _hasContext;
		protected Coroutine _spawnRoutine;

		public virtual void Initialize(SkillRuntimeContext context)
		{
			_context = context;
			_hasContext = true;

			if (StartOnInitialize)
			{
				StartBurst();
			}
		}

		public virtual void StartBurst()
		{
			if (_spawnRoutine != null)
			{
				StopCoroutine(_spawnRoutine);
			}

			_spawnRoutine = StartCoroutine(SpawnSequence());
		}

		protected virtual IEnumerator SpawnSequence()
		{
			if (SpawnPrefab == null)
			{
				if (LogDebug)
				{
					Debug.LogWarning("SkillBurstSpawner2D has no SpawnPrefab assigned.", this);
				}

				yield break;
			}

			if (InitialSpawnDelay > 0f)
			{
				yield return new WaitForSeconds(InitialSpawnDelay);
			}

			for (int i = 0; i < SpawnCount; i++)
			{
				SpawnAtIndex(i);

				if (DelayBetweenSpawns > 0f && i < SpawnCount - 1)
				{
					yield return new WaitForSeconds(DelayBetweenSpawns);
				}
			}

			_spawnRoutine = null;

			if (DestroyWhenFinished)
			{
				Destroy(gameObject, DestroyDelayAfterFinished);
			}
		}

		protected virtual void SpawnAtIndex(int index)
		{
			bool facingRight = !_hasContext || _context.FacingRight;
			Vector2 offset = StartOffset + (StepOffset * index);

			if (MatchOwnerFacing)
			{
				offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);
			}

			Transform origin = SpawnOrigin != null ? SpawnOrigin : transform;
			Transform parent = ParentSpawnedObjectsToOrigin ? origin : null;
			GameObject instance = Instantiate(SpawnPrefab, origin.position + (Vector3)offset, Quaternion.identity, parent);

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
	}
}
