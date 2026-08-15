using System.Collections;
using MaskboundJinosi.Skills;
using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Timed Skill Object")]
	public class TimedSkillObject : MonoBehaviour, ISkillRuntimeReceiver
	{
		[Min(0f)] public float LifetimeOverride;
		public bool DestroyOnEnd = true;
		[Tooltip("Jika aktif, LifetimeOverride tetap berjalan meski object tidak dibuat oleh sistem SkillRuntimeContext (contoh: projectile boss).")]
		public bool StartAutomatically = true;

		protected Coroutine _lifetimeCoroutine;
		protected bool _initializedByContext;

		protected virtual void Start()
		{
			if (!_initializedByContext && StartAutomatically && LifetimeOverride > 0f)
			{
				StartLifetime(LifetimeOverride);
			}
		}

		public virtual void Initialize(SkillRuntimeContext context)
		{
			_initializedByContext = true;
			float lifetime = LifetimeOverride > 0f ? LifetimeOverride : context.Duration;
			if (lifetime <= 0f)
			{
				return;
			}

			StartLifetime(lifetime);
		}

		public virtual void StartLifetime(float lifetime)
		{
			if (lifetime <= 0f)
			{
				return;
			}

			if (_lifetimeCoroutine != null)
			{
				StopCoroutine(_lifetimeCoroutine);
			}

			_lifetimeCoroutine = StartCoroutine(LifetimeCo(lifetime));
		}

		protected virtual IEnumerator LifetimeCo(float lifetime)
		{
			yield return new WaitForSeconds(lifetime);

			if (DestroyOnEnd)
			{
				Destroy(gameObject);
			}
			else
			{
				gameObject.SetActive(false);
			}
		}
	}
}
