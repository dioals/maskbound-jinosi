using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
	[AddComponentMenu("Maskbound/Combat/Damage Number Manager")]
	public class DamageNumberManager : MonoBehaviour, MMEventListener<MMDamageTakenEvent>
	{
		[Header("References")]
		public DamageNumberPopup TextPrefab;

		[Header("Spawn")]
		public Vector2 SpawnOffset = new Vector2(0f, 1f);
		public bool IgnorePlayerDamage = true;

		protected virtual void OnEnable()
		{
			this.MMEventStartListening<MMDamageTakenEvent>();
		}

		protected virtual void OnDisable()
		{
			this.MMEventStopListening<MMDamageTakenEvent>();
		}

		public virtual void OnMMEvent(MMDamageTakenEvent damageEvent)
		{
			if (TextPrefab == null || damageEvent.AffectedHealth == null || damageEvent.DamageCaused <= 0f)
			{
				return;
			}

			if (IgnorePlayerDamage)
			{
				Character targetCharacter = damageEvent.AffectedHealth.GetComponentInParent<Character>();
				if (targetCharacter != null && targetCharacter.CharacterType == Character.CharacterTypes.Player)
				{
					return;
				}
			}

			Transform targetTransform = damageEvent.AffectedHealth.transform;
			Vector3 spawnPosition = targetTransform.position + (Vector3)SpawnOffset;
			float horizontalDirection = ComputeAwayFromPlayerDirection(targetTransform.position);

			DamageNumberPopup popup = Instantiate(TextPrefab, spawnPosition, Quaternion.identity);
			popup.Initialize(Mathf.RoundToInt(damageEvent.DamageCaused), horizontalDirection);
		}

		protected virtual float ComputeAwayFromPlayerDirection(Vector3 targetPosition)
		{
			if (!LevelManager.HasInstance || LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0)
			{
				return 0f;
			}

			Character player = LevelManager.Instance.Players[0];
			if (player == null)
			{
				return 0f;
			}

			return targetPosition.x - player.transform.position.x;
		}
	}
}
