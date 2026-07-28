using MaskboundJinosi.Skills;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Skill Summon Object")]
	public class SkillSummonObject : MonoBehaviour, ISkillRuntimeReceiver
	{
		public Transform OwnerFollowTarget;
		public Vector2 FollowOffset = new Vector2(1.5f, 0.5f);
		[Min(0f)] public float FollowSpeed = 8f;
		public bool FollowOwner;

		protected SkillRuntimeContext _context;
		protected Transform _ownerTransform;

		public virtual void Initialize(SkillRuntimeContext context)
		{
			_context = context;
			_ownerTransform = context.Owner != null ? context.Owner.transform : null;
		}

		protected virtual void LateUpdate()
		{
			if (!FollowOwner || _ownerTransform == null)
			{
				return;
			}

			Transform target = OwnerFollowTarget != null ? OwnerFollowTarget : _ownerTransform;
			Vector2 offset = FollowOffset;
			offset.x = Mathf.Abs(offset.x) * (_context.FacingRight ? 1f : -1f);
			Vector3 targetPosition = target.position + (Vector3)offset;
			transform.position = Vector3.Lerp(transform.position, targetPosition, FollowSpeed * Time.deltaTime);
		}
	}
}
