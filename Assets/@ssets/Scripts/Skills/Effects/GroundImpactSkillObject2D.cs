using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Ground Impact Skill Object 2D")]
	[RequireComponent(typeof(Rigidbody2D))]
	public class GroundImpactSkillObject2D : MonoBehaviour
	{
		[Header("Spawn To Fall")]
		[SerializeField] private bool startAsKinematic = true;
		[SerializeField] private float fallGravityScale = 1f;
		[SerializeField] private Vector2 releaseVelocity = Vector2.zero;

		[Header("Impact")]
		[SerializeField] private LayerMask groundLayerMask;
		[SerializeField] private string impactTrigger = "Impact";
		[SerializeField] private bool freezeOnImpact = true;
		[SerializeField] private bool disableColliderOnImpact;
		[SerializeField] private bool enableDamageOnImpact = true;
		[SerializeField] private float damageActiveDuration = 0.15f;
		[SerializeField] private bool destroyAfterImpact = true;
		[SerializeField] private float destroyDelay = 0.5f;
		[SerializeField] private bool logImpact;

		private Rigidbody2D _rigidbody2D;
		private Collider2D _collider2D;
		private Animator _animator;
		private DamageOnTouch _damageOnTouch;
		private bool _hasImpacted;

		private void Awake()
		{
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_collider2D = GetComponent<Collider2D>();
			_animator = GetComponentInChildren<Animator>();
			_damageOnTouch = GetComponent<DamageOnTouch>();

			if (startAsKinematic)
			{
				_rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
				_rigidbody2D.linearVelocity = Vector2.zero;
				_rigidbody2D.angularVelocity = 0f;
				_rigidbody2D.gravityScale = 0f;
				_rigidbody2D.simulated = true;
			}

			if (_damageOnTouch != null && enableDamageOnImpact)
			{
				_damageOnTouch.enabled = false;
			}
		}

		public void ReleaseToDynamic()
		{
			if (_hasImpacted)
			{
				return;
			}

			_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
			_rigidbody2D.gravityScale = fallGravityScale;
			_rigidbody2D.linearVelocity = releaseVelocity;
			_rigidbody2D.angularVelocity = 0f;
			_rigidbody2D.simulated = true;
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			TryImpact(collision.collider);
		}

		private void OnTriggerEnter2D(Collider2D other)
		{
			TryImpact(other);
		}

		private void TryImpact(Collider2D other)
		{
			if (_hasImpacted || other == null || !IsInLayerMask(other.gameObject.layer, groundLayerMask))
			{
				return;
			}

			_hasImpacted = true;

			if (freezeOnImpact)
			{
				_rigidbody2D.linearVelocity = Vector2.zero;
				_rigidbody2D.angularVelocity = 0f;
				_rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
				_rigidbody2D.simulated = true;
			}

			if (_animator != null && !string.IsNullOrWhiteSpace(impactTrigger))
			{
				_animator.SetTrigger(impactTrigger);
			}

			if (disableColliderOnImpact && _collider2D != null)
			{
				_collider2D.enabled = false;
			}

			if (logImpact)
			{
				Debug.Log($"{name} impacted {other.name}.", this);
			}

			if (_damageOnTouch != null && enableDamageOnImpact)
			{
				StartCoroutine(DamageWindow());
			}

			if (destroyAfterImpact)
			{
				Destroy(gameObject, destroyDelay);
			}
		}

		private IEnumerator DamageWindow()
		{
			_damageOnTouch.enabled = true;

			if (damageActiveDuration <= 0f)
			{
				yield break;
			}

			yield return new WaitForSeconds(damageActiveDuration);

			if (_damageOnTouch != null)
			{
				_damageOnTouch.enabled = false;
			}
		}

		private static bool IsInLayerMask(int layer, LayerMask layerMask)
		{
			return (layerMask.value & (1 << layer)) != 0;
		}
	}
}
