using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Skill Selection Icon Feedback")]
	public class SkillSelectionIconFeedback : MonoBehaviour
	{
		[Header("References")]
		public SkillIconFloatingPopup IconPrefab;
		public Transform SpawnOrigin;

		[Header("Spawn")]
		public Vector2 SpawnOffset = new Vector2(0f, 2.3f);

		protected virtual void Awake()
		{
			if (SpawnOrigin == null)
			{
				SpawnOrigin = transform;
			}
		}

		public virtual void Show(Sprite icon)
		{
			if (IconPrefab == null || icon == null)
			{
				return;
			}

			Transform origin = SpawnOrigin != null ? SpawnOrigin : transform;
			Vector3 spawnPosition = origin.position + (Vector3)SpawnOffset;
			SkillIconFloatingPopup instance = Instantiate(IconPrefab, spawnPosition, Quaternion.identity);

			if (instance != null)
			{
				instance.SetIcon(icon);
			}
		}
	}
}
