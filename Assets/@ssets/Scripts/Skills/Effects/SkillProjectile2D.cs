using MaskboundJinosi.Skills;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Skill Projectile 2D")]
	[RequireComponent(typeof(Rigidbody2D))]
	public class SkillProjectile2D : MonoBehaviour, ISkillRuntimeReceiver
	{
		[Min(0f)] public float Speed = 10f;
		public Vector2 Direction = Vector2.right;
		public bool UseFacingDirection = true;
		public bool RotateToDirection;

		protected Rigidbody2D _rigidbody2D;

		protected virtual void Awake()
		{
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
			_rigidbody2D.simulated = true;
		}

		public virtual void Initialize(SkillRuntimeContext context)
		{
			Vector2 direction = Direction.sqrMagnitude > 0f ? Direction.normalized : Vector2.right;
			if (UseFacingDirection)
			{
				direction.x = Mathf.Abs(direction.x) * (context.FacingRight ? 1f : -1f);
			}

			_rigidbody2D.linearVelocity = direction * Speed;

			if (RotateToDirection && direction.sqrMagnitude > 0f)
			{
				float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
				transform.rotation = Quaternion.Euler(0f, 0f, angle);
			}
		}
	}
}
