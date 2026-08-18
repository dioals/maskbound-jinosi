using UnityEngine;

namespace MaskboundJinosi.Breakables
{
	[AddComponentMenu("Maskbound/World/Breakable Soul Object")]
	public class BreakableSoulObject : BreakableObject
	{
		[Header("Reward")]
		public GameObject SoulPickupPrefab;
		public int SoulPickupCount = 1;
		public Vector2 PickupSpawnOffset = new Vector2(0f, 0.5f);

		protected override void SpawnReward()
		{
			SpawnSoulPickups();
		}

		protected virtual void SpawnSoulPickups()
		{
			if (SoulPickupPrefab == null)
			{
				return;
			}

			int count = Mathf.Max(0, SoulPickupCount);
			for (int i = 0; i < count; i++)
			{
				Vector3 offset = new Vector3(PickupSpawnOffset.x, PickupSpawnOffset.y, 0f);
				if (count > 1)
				{
					offset.x += (i - ((count - 1) * 0.5f)) * 0.35f;
				}

				Instantiate(SoulPickupPrefab, transform.position + offset, Quaternion.identity);
			}
		}
	}
}
