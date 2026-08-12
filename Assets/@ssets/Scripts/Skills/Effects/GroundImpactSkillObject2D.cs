using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Ground Impact Skill Object 2D")]
	[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
	public class GroundImpactSkillObject2D : MonoBehaviour
	{
		[Header("Visual Fall")]
		[Tooltip("Child yang berisi Animator dan SpriteRenderer. Jika kosong, dicari otomatis.")]
		[SerializeField] private Transform visualRoot;
		[SerializeField, Min(0f)] private float visualStartHeight = 10f;
		[SerializeField, Min(0.01f)] private float fallDuration = 0.35f;
		[SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		[SerializeField] private Vector2 visualImpactOffset;

		[Header("Impact")]
		[SerializeField] private string impactTrigger = "Impact";
		[SerializeField] private bool enableDamageOnImpact = true;
		[SerializeField, Min(0f)] private float damageActiveDuration = 0.15f;
		[SerializeField] private bool destroyAfterImpact = true;
		[SerializeField, Min(0f)] private float destroyDelay = 0.5f;
		[SerializeField] private bool logImpact;

		private Rigidbody2D _rigidbody2D;
		private Collider2D _collider2D;
		private Animator _animator;
		private DamageOnTouch _damageOnTouch;
		private Vector3 _visualImpactLocalPosition;
		private Coroutine _fallRoutine;
		private bool _hasImpacted;
		public bool HasImpacted => _hasImpacted;

		private void Awake()
		{
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_collider2D = GetComponent<Collider2D>();
			_damageOnTouch = GetComponent<DamageOnTouch>();

			if (visualRoot == null)
			{
				_animator = GetComponentInChildren<Animator>();
				visualRoot = _animator != null ? _animator.transform : null;
			}
			else
			{
				_animator = visualRoot.GetComponentInChildren<Animator>();
			}

			_rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
			_rigidbody2D.gravityScale = 0f;
			_rigidbody2D.linearVelocity = Vector2.zero;
			_rigidbody2D.angularVelocity = 0f;
			_rigidbody2D.simulated = true;

			if (visualRoot != null)
			{
				_visualImpactLocalPosition = visualRoot.localPosition + (Vector3)visualImpactOffset;
				visualRoot.localPosition = _visualImpactLocalPosition + Vector3.up * visualStartHeight;
			}

			_collider2D.enabled = false;
			if (_damageOnTouch != null)
			{
				_damageOnTouch.enabled = false;
			}
		}

		public void BeginFall()
		{
			if (_hasImpacted || _fallRoutine != null)
			{
				return;
			}

			_fallRoutine = StartCoroutine(FallVisual());
		}

		// Alias untuk Animation Event lama pada clip Spawn.
		public void ReleaseToDynamic() => BeginFall();

		private IEnumerator FallVisual()
		{
			if (visualRoot == null)
			{
				Impact();
				yield break;
			}

			Vector3 start = _visualImpactLocalPosition + Vector3.up * visualStartHeight;
			float elapsed = 0f;
			while (elapsed < fallDuration)
			{
				elapsed += Time.deltaTime;
				float progress = Mathf.Clamp01(elapsed / fallDuration);
				float evaluated = fallCurve != null ? fallCurve.Evaluate(progress) : progress;
				visualRoot.localPosition = Vector3.LerpUnclamped(start, _visualImpactLocalPosition, evaluated);
				yield return null;
			}

			visualRoot.localPosition = _visualImpactLocalPosition;
			Impact();
		}

		private void Impact()
		{
			if (_hasImpacted)
			{
				return;
			}

			_hasImpacted = true;
			_fallRoutine = null;
			_collider2D.enabled = true;

			if (_animator != null && !string.IsNullOrWhiteSpace(impactTrigger))
			{
				_animator.SetTrigger(impactTrigger);
			}

			if (logImpact)
			{
				Debug.Log($"{name} reached its fixed impact point.", this);
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
			if (damageActiveDuration > 0f)
			{
				yield return new WaitForSeconds(damageActiveDuration);
			}

			if (_damageOnTouch != null)
			{
				_damageOnTouch.enabled = false;
			}
		}
	}
}
