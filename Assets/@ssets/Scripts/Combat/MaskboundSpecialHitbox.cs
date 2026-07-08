using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
	[AddComponentMenu("Maskbound/Combat/Special Hitbox")]
	[RequireComponent(typeof(BoxCollider2D))]
	[RequireComponent(typeof(Rigidbody2D))]
	[RequireComponent(typeof(DamageOnTouch))]
	public class MaskboundSpecialHitbox : MonoBehaviour
	{
		protected BoxCollider2D _boxCollider;
		protected Rigidbody2D _rigidbody2D;
		protected DamageOnTouch _damageOnTouch;

		protected virtual void Awake()
		{
			_boxCollider = GetComponent<BoxCollider2D>();
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_damageOnTouch = GetComponent<DamageOnTouch>();

			_boxCollider.isTrigger = true;
			_rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
			_rigidbody2D.simulated = true;
		}

		public virtual void Configure(MaskboundSpecialAttackData data, GameObject owner, bool facingRight)
		{
			if (data == null)
			{
				return;
			}

			Vector2 offset = data.HitboxOffset;
			offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);

			_boxCollider.offset = offset;
			_boxCollider.size = data.HitboxSize;

			_damageOnTouch.Owner = owner;
			_damageOnTouch.TargetLayerMask = data.TargetLayerMask;
			_damageOnTouch.MinDamageCaused = data.MinDamage;
			_damageOnTouch.MaxDamageCaused = Mathf.Max(data.MinDamage, data.MaxDamage);
			_damageOnTouch.DamageCausedKnockbackType = data.KnockbackType;
			_damageOnTouch.DamageCausedKnockbackForce = data.KnockbackForce;
			_damageOnTouch.InvincibilityDuration = data.InvincibilityDuration;
			_damageOnTouch.ApplyDamageOnTriggerEnter = true;
			_damageOnTouch.ApplyDamageOnTriggerStay = false;
		}

		public virtual void Activate()
		{
			gameObject.SetActive(true);
		}

		public virtual void Deactivate()
		{
			gameObject.SetActive(false);
		}
	}
}
