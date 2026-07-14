using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Health Text Display")]
	public class HealthTextDisplay : MonoBehaviour, MMEventListener<HealthChangeEvent>
	{
		public enum HealthTextFormat
		{
			CurrentOnly,
			CurrentAndMax,
			Percent
		}

		[Header("References")]
		public TMP_Text TargetText;
		public Health TargetHealth;

		[Header("Auto Find")]
		public bool AutoFindPlayerHealth = true;
		public string PlayerTag = "Player";

		[Header("Format")]
		public string Prefix = "HP: ";
		public string Suffix = "";
		public HealthTextFormat Format = HealthTextFormat.CurrentAndMax;
		public bool RoundToWholeNumber = true;

		protected virtual void Reset()
		{
			TargetText = GetComponent<TMP_Text>();
		}

		protected virtual void Awake()
		{
			if (TargetText == null)
			{
				TargetText = GetComponent<TMP_Text>();
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
			if (TargetText == null)
			{
				return;
			}

			if (TargetHealth == null)
			{
				TargetText.text = $"{Prefix}-{Suffix}";
				return;
			}

			TargetText.text = $"{Prefix}{GetFormattedHealth()}{Suffix}";
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

		protected virtual string GetFormattedHealth()
		{
			float current = TargetHealth.CurrentHealth;
			float maximum = TargetHealth.MaximumHealth;

			if (RoundToWholeNumber)
			{
				current = Mathf.Round(current);
				maximum = Mathf.Round(maximum);
			}

			switch (Format)
			{
				case HealthTextFormat.CurrentOnly:
					return FormatNumber(current);
				case HealthTextFormat.Percent:
					if (maximum <= 0f)
					{
						return "0%";
					}
					return $"{Mathf.RoundToInt((TargetHealth.CurrentHealth / maximum) * 100f)}%";
				case HealthTextFormat.CurrentAndMax:
				default:
					return $"{FormatNumber(current)} / {FormatNumber(maximum)}";
			}
		}

		protected virtual string FormatNumber(float value)
		{
			return RoundToWholeNumber ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##");
		}
	}
}
