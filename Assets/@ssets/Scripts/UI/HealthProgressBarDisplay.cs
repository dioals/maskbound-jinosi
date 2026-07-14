using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Health Progress Bar Display")]
	public class HealthProgressBarDisplay : MonoBehaviour, MMEventListener<HealthChangeEvent>
	{
		[Header("References")]
		public MMProgressBar TargetProgressBar;
		public Health TargetHealth;

		[Header("Auto Find")]
		public bool AutoFindPlayerHealth = true;
		public string PlayerTag = "Player";

		protected virtual void Reset()
		{
			TargetProgressBar = GetComponent<MMProgressBar>();
		}

		protected virtual void Awake()
		{
			if (TargetProgressBar == null)
			{
				TargetProgressBar = GetComponent<MMProgressBar>();
			}

			ResolveHealthReference();
		}

		protected virtual void OnEnable()
		{
			this.MMEventStartListening<HealthChangeEvent>();
			ResolveHealthReference();
			Refresh();
		}

		protected virtual void OnDisable()
		{
			this.MMEventStopListening<HealthChangeEvent>();
		}

		public virtual void OnMMEvent(HealthChangeEvent healthChangeEvent)
		{
			if (TargetHealth == null)
			{
				ResolveHealthReference();
			}

			if (healthChangeEvent.AffectedHealth != TargetHealth)
			{
				return;
			}

			Refresh();
		}

		public virtual void SetTargetHealth(Health health)
		{
			TargetHealth = health;
			Refresh();
		}

		public virtual void Refresh()
		{
			if (TargetProgressBar == null)
			{
				return;
			}

			if (TargetHealth == null)
			{
				TargetProgressBar.UpdateBar(0f, 0f, 1f);
				return;
			}

			TargetProgressBar.UpdateBar(TargetHealth.CurrentHealth, 0f, TargetHealth.MaximumHealth);
		}

		protected virtual void ResolveHealthReference()
		{
			if (TargetHealth != null || !AutoFindPlayerHealth)
			{
				return;
			}

			GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
			if (player == null)
			{
				return;
			}

			TargetHealth = player.GetComponent<Health>();
		}
	}
}
