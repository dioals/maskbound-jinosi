using System.Collections;
using MoreMountains.CorgiEngine;
using MaskboundJinosi.AI;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Hammer Rain Bomb Damageable")]
	[RequireComponent(typeof(Health), typeof(Collider2D))]
	public class HammerRainBombDamageable : MonoBehaviour
	{
		[SerializeField] private Health health;
		[SerializeField] private GroundImpactSkillObject2D groundImpact;
		[SerializeField] private Animator animator;
		[SerializeField, Min(0f)] private float idleLifetime = 5f;
		[SerializeField, Min(0f)] private float destroyAnimationDuration = 0.6f;
		[SerializeField] private string hitTrigger = "Hit";
		[SerializeField] private string destroyTrigger = "Destroy";
		[SerializeField, Min(0f)] private float bossStunDuration = 7f;

		private Collider2D _collider;
		private DamageOnTouch _damageOnTouch;
		private Coroutine _lifetimeRoutine;
		private bool _resolved;

		private void Awake()
		{
			health ??= GetComponent<Health>();
			groundImpact ??= GetComponent<GroundImpactSkillObject2D>();
			animator ??= GetComponentInChildren<Animator>();
			_collider = GetComponent<Collider2D>();
			_damageOnTouch = GetComponent<DamageOnTouch>();
		}

		private void OnEnable()
		{
			if (health != null)
			{
				health.OnHit += HandleHit;
				health.OnDeath += HandleDeath;
			}
			_lifetimeRoutine = StartCoroutine(LifetimeAfterImpact());
		}

		private void OnDisable()
		{
			if (health != null)
			{
				health.OnHit -= HandleHit;
				health.OnDeath -= HandleDeath;
			}
		}

		private IEnumerator LifetimeAfterImpact()
		{
			while (groundImpact != null && !groundImpact.HasImpacted)
			{
				yield return null;
			}
			if (idleLifetime > 0f)
			{
				yield return new WaitForSeconds(idleLifetime);
			}
			ResolveBomb();
		}

		private void HandleHit()
		{
			if (_resolved || health == null || health.CurrentHealth <= 0f)
			{
				return;
			}
			SetTrigger(hitTrigger);
		}

		private void HandleDeath()
		{
			ApplyBossWeakness();
			ResolveBomb();
		}

		private void ApplyBossWeakness()
		{
			BossStunReceiver[] receivers = FindObjectsByType<BossStunReceiver>(FindObjectsSortMode.None);
			if (receivers.Length == 0) { return; }

			BossStunReceiver nearest = receivers[0];
			float nearestDistance = (nearest.transform.position - transform.position).sqrMagnitude;
			for (int i = 1; i < receivers.Length; i++)
			{
				float distance = (receivers[i].transform.position - transform.position).sqrMagnitude;
				if (distance < nearestDistance)
				{
					nearest = receivers[i];
					nearestDistance = distance;
				}
			}
			nearest.StunFor(bossStunDuration);
		}

		private void ResolveBomb()
		{
			if (_resolved) { return; }
			_resolved = true;
			if (_lifetimeRoutine != null)
			{
				StopCoroutine(_lifetimeRoutine);
				_lifetimeRoutine = null;
			}
			if (health != null) { health.Invulnerable = true; }
			if (_damageOnTouch != null) { _damageOnTouch.enabled = false; }
			if (_collider != null) { _collider.enabled = false; }
			SetTrigger(destroyTrigger);
			Destroy(gameObject, destroyAnimationDuration);
		}

		private void SetTrigger(string parameter)
		{
			if (animator != null && !string.IsNullOrWhiteSpace(parameter))
			{
				animator.SetTrigger(parameter);
			}
		}
	}
}
