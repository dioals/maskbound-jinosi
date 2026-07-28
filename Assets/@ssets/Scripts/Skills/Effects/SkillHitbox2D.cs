using MaskboundJinosi.Skills;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Skill Hitbox 2D")]
	[RequireComponent(typeof(BoxCollider2D))]
	[RequireComponent(typeof(Rigidbody2D))]
	[RequireComponent(typeof(DamageOnTouch))]
	public class SkillHitbox2D : MonoBehaviour, ISkillRuntimeReceiver
	{
		public LayerMask TargetLayerMask;
		public bool OverrideDamage;
		[Min(0f)] public float MinDamage;
		[Min(0f)] public float MaxDamage;
		public bool ApplyDamageOnTriggerEnter = true;
		public bool ApplyDamageOnTriggerStay;
		[Min(0f)] public float InvincibilityDuration = 0.2f;

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

		public virtual void Initialize(SkillRuntimeContext context)
		{
			float damage = OverrideDamage ? MinDamage : context.Damage;
			float maxDamage = OverrideDamage ? Mathf.Max(MinDamage, MaxDamage) : context.Damage;

			_damageOnTouch.Owner = context.Owner;
			if (TargetLayerMask.value != 0)
			{
				_damageOnTouch.TargetLayerMask = TargetLayerMask;
			}
			_damageOnTouch.MinDamageCaused = damage;
			_damageOnTouch.MaxDamageCaused = maxDamage;
			_damageOnTouch.InvincibilityDuration = InvincibilityDuration;
			_damageOnTouch.ApplyDamageOnTriggerEnter = ApplyDamageOnTriggerEnter;
			_damageOnTouch.ApplyDamageOnTriggerStay = ApplyDamageOnTriggerStay;
		}
	}
}
