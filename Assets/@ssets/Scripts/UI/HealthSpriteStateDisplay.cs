using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Health Sprite State Display")]
	public class HealthSpriteStateDisplay : MonoBehaviour, MMEventListener<HealthChangeEvent>
	{
		[System.Serializable]
		public class HealthSpriteState
		{
			[Range(0f, 100f)]
			public float MinimumPercent = 100f;
			public Sprite Sprite;
		}

		[Header("References")]
		public Image TargetImage;
		public Health TargetHealth;

		[Header("Auto Find")]
		public bool AutoFindPlayerHealth = true;
		public string PlayerTag = "Player";

		[Header("States")]
		public HealthSpriteState[] SpriteStates =
		{
			new HealthSpriteState { MinimumPercent = 100f },
			new HealthSpriteState { MinimumPercent = 80f },
			new HealthSpriteState { MinimumPercent = 50f },
			new HealthSpriteState { MinimumPercent = 30f }
		};

		protected virtual void Reset()
		{
			TargetImage = GetComponent<Image>();
		}

		protected virtual void Awake()
		{
			if (TargetImage == null)
			{
				TargetImage = GetComponent<Image>();
			}

			ResolveHealthReference();
			SortStatesDescending();
		}

		protected virtual void OnEnable()
		{
			this.MMEventStartListening<HealthChangeEvent>();
			ResolveHealthReference();
			SortStatesDescending();
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
			if (TargetImage == null || TargetHealth == null || SpriteStates == null || SpriteStates.Length == 0)
			{
				return;
			}

			float healthPercent = GetHealthPercent();
			Sprite selectedSprite = null;

			for (int i = 0; i < SpriteStates.Length; i++)
			{
				if (SpriteStates[i] == null)
				{
					continue;
				}

				if (healthPercent >= SpriteStates[i].MinimumPercent)
				{
					selectedSprite = SpriteStates[i].Sprite;
					break;
				}
			}

			if (selectedSprite == null)
			{
				selectedSprite = GetLowestStateSprite();
			}

			if (selectedSprite != null)
			{
				TargetImage.sprite = selectedSprite;
			}
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

		protected virtual float GetHealthPercent()
		{
			if (TargetHealth.MaximumHealth <= 0f)
			{
				return 0f;
			}

			return Mathf.Clamp01(TargetHealth.CurrentHealth / TargetHealth.MaximumHealth) * 100f;
		}

		protected virtual Sprite GetLowestStateSprite()
		{
			HealthSpriteState lowestState = null;

			for (int i = 0; i < SpriteStates.Length; i++)
			{
				if (SpriteStates[i] == null)
				{
					continue;
				}

				if (lowestState == null || SpriteStates[i].MinimumPercent < lowestState.MinimumPercent)
				{
					lowestState = SpriteStates[i];
				}
			}

			return lowestState?.Sprite;
		}

		protected virtual void SortStatesDescending()
		{
			if (SpriteStates == null)
			{
				return;
			}

			System.Array.Sort(SpriteStates, (left, right) =>
			{
				if (left == null && right == null)
				{
					return 0;
				}

				if (left == null)
				{
					return 1;
				}

				if (right == null)
				{
					return -1;
				}

				return right.MinimumPercent.CompareTo(left.MinimumPercent);
			});
		}
	}
}
