using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Skill Cooldown Feedback")]
	public class SkillCooldownFeedback : MonoBehaviour
	{
		[Header("References")]
		public CooldownFloatingText TextPrefab;
		public Transform SpawnOrigin;

		[Header("Spawn")]
		public Vector2 SpawnOffset = new Vector2(0f, 1.6f);

		protected virtual void Awake()
		{
			if (SpawnOrigin == null)
			{
				SpawnOrigin = transform;
			}
		}

		public virtual void Show()
		{
			if (TextPrefab == null)
			{
				return;
			}

			Transform origin = SpawnOrigin != null ? SpawnOrigin : transform;
			Vector3 spawnPosition = origin.position + (Vector3)SpawnOffset;
			Instantiate(TextPrefab, spawnPosition, Quaternion.identity);
		}
	}
}
