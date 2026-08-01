using MoreMountains.CorgiEngine;
using UnityEngine;

namespace Maskbound.FX
{
	[AddComponentMenu("Maskbound/FX/Spawned VFX Facing 2D")]
	public class SpawnedVFXFacing2D : MonoBehaviour
	{
		[Tooltip("Isi jika VFX sudah menjadi child player. Untuk feedback instantiate, biarkan kosong.")]
		[SerializeField] private Character character;

		[Tooltip("Aktifkan jika sprite VFX aslinya menghadap arah yang berlawanan dari player.")]
		[SerializeField] private bool invertFacing;

		[Tooltip("Jika Character belum diisi, cari Character terdekat setelah VFX ditempatkan oleh feedback.")]
		[SerializeField] private bool findNearestCharacter = true;

		private float _absoluteScaleX;

		private void Awake()
		{
			_absoluteScaleX = Mathf.Abs(transform.localScale.x);
		}

		private void Start()
		{
			// MMF_InstantiateObject baru mengatur posisi dan scale setelah Instantiate/Awake selesai.
			_absoluteScaleX = Mathf.Abs(transform.localScale.x);

			if (character == null)
			{
				character = GetComponentInParent<Character>();
			}

			if (character == null && findNearestCharacter)
			{
				character = FindNearestCharacter();
			}

			ApplyFacing();
		}

		public void ApplyFacing()
		{
			if (character == null)
			{
				return;
			}

			bool faceRight = invertFacing ? !character.IsFacingRight : character.IsFacingRight;
			Vector3 scale = transform.localScale;
			scale.x = _absoluteScaleX * (faceRight ? 1f : -1f);
			transform.localScale = scale;
		}

		private Character FindNearestCharacter()
		{
			Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
			Character nearest = null;
			float nearestSqrDistance = float.PositiveInfinity;

			foreach (Character candidate in characters)
			{
				if (candidate == null || !candidate.gameObject.activeInHierarchy)
				{
					continue;
				}

				float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
				if (sqrDistance < nearestSqrDistance)
				{
					nearest = candidate;
					nearestSqrDistance = sqrDistance;
				}
			}

			return nearest;
		}
	}
}
