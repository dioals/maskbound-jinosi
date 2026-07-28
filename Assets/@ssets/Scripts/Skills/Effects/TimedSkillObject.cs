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

		protected Coroutine _lifetimeCoroutine;

		public virtual void Initialize(SkillRuntimeContext context)
		{
			float lifetime = LifetimeOverride > 0f ? LifetimeOverride : context.Duration;
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
