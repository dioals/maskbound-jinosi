using MoreMountains.CorgiEngine;
using UnityEngine;

namespace Maskbound.FX
{
	[AddComponentMenu("Maskbound/FX/Facing Spawn Point 2D")]
	public class FacingSpawnPoint2D : MonoBehaviour
	{
		public Character Character;
		public Vector2 RightFacingLocalPosition = new Vector2(1f, 0f);
		public bool MirrorXWhenFacingLeft = true;
		public bool UpdateEveryFrame = true;

		protected virtual void Awake()
		{
			if (Character == null)
			{
				Character = GetComponentInParent<Character>();
			}

			ApplyFacingPosition();
		}

		protected virtual void LateUpdate()
		{
			if (UpdateEveryFrame)
			{
				ApplyFacingPosition();
			}
		}

		public virtual void ApplyFacingPosition()
		{
			Vector3 position = RightFacingLocalPosition;

			if (MirrorXWhenFacingLeft && Character != null && !Character.IsFacingRight)
			{
				position.x = -Mathf.Abs(position.x);
			}
			else
			{
				position.x = Mathf.Abs(position.x);
			}

			transform.localPosition = position;
		}
	}
}
