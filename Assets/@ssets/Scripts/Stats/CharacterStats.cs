using System;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Stats
{
	[AddComponentMenu("Maskbound/Stats/Character Stats")]
	[RequireComponent(typeof(Health))]
	public class CharacterStats : MonoBehaviour
	{
		[Header("Data")]
		public CharacterStatData StatData;

		[Header("Runtime")]
		[SerializeField] protected float _attackDamageBonus;

		protected Health _health;

		public event Action<float> AttackDamageChanged;

		public Health Health => _health;
		public float CurrentHealth => _health != null ? _health.CurrentHealth : 0f;
		public float MaximumHealth => _health != null ? _health.MaximumHealth : 0f;
		public float BaseAttackDamage => StatData != null ? StatData.BaseAttackDamage : 10f;
		public float AttackDamage => Mathf.Max(0f, BaseAttackDamage + _attackDamageBonus);
		public float AttackDamageBonus => _attackDamageBonus;

		protected virtual void Awake()
		{
			_health = GetComponent<Health>();
		}

		public virtual float GetAttackDamage()
		{
			return AttackDamage;
		}

		public virtual void SetAttackDamageBonus(float bonus)
		{
			_attackDamageBonus = bonus;
			AttackDamageChanged?.Invoke(AttackDamage);
		}

		public virtual void AddAttackDamageBonus(float bonus)
		{
			SetAttackDamageBonus(_attackDamageBonus + bonus);
		}
	}
}
