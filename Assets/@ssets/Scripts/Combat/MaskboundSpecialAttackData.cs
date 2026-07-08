using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
	[CreateAssetMenu(fileName = "SpecialAttackData", menuName = "Maskbound/Combat/Special Attack Data")]
	public class MaskboundSpecialAttackData : ScriptableObject
	{
		[Header("Identity")]
		public string DisplayName = "Panji Spirit Burst";

		[Header("Cost")]
		public float Cooldown = 5f;
		public float EnergyCost = 30f;
		public bool RequiresEnergy = false;

		[Header("Timing")]
		public float StartupTime = 0.15f;
		public float ActiveTime = 0.25f;
		public float RecoveryTime = 0.35f;

		[Header("Hitbox")]
		public Vector2 HitboxOffset = new Vector2(1.1f, 0f);
		public Vector2 HitboxSize = new Vector2(1.8f, 1.2f);
		public LayerMask TargetLayerMask;

		[Header("Damage")]
		public float MinDamage = 25f;
		public float MaxDamage = 25f;
		public DamageOnTouch.KnockbackStyles KnockbackType = DamageOnTouch.KnockbackStyles.SetForce;
		public Vector2 KnockbackForce = new Vector2(18f, 4f);
		public float InvincibilityDuration = 0.25f;

		[Header("Animation")]
		public string SpecialAttackTriggerParameter = "SpecialAttack";
		public string IsSpecialAttackingParameter = "IsSpecialAttacking";
		public string SpecialIndexParameter = "SpecialIndex";
		public int SpecialIndex = 1;

		public float TotalDuration => Mathf.Max(0f, StartupTime) + Mathf.Max(0f, ActiveTime) + Mathf.Max(0f, RecoveryTime);
	}
}
