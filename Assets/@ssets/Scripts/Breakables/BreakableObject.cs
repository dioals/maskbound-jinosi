using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Breakables
{
	/// <summary>
	/// Shared root for every breakable object (soul Kendi, stone, ...).
	/// Handles player-damage detection, the break effect and the optional
	/// "stays broken across saves" persistence. Subclasses only add their own
	/// reward / animation behavior.
	/// </summary>
	public class BreakableObject : MonoBehaviour
	{
		/// <summary>
		/// PlayerPrefs key holding the comma-separated list of SaveIds of every
		/// breakable that has already been destroyed in the current save.
		/// Wiped by GameFlowManager.ClearSaveData() on New Game.
		/// </summary>
		public const string BrokenBreakablesKey = "Maskbound.BrokenBreakables";

		[Header("Break")]
		public GameObject BreakEffectPrefab;
		public bool DisableOnBreak = true;
		public bool DestroyOnBreak = false;
		public float DestroyDelay = 0f;

		[Header("Persistence")]
		[Tooltip("Unique ID for this breakable (e.g. \"forest_stone_1\"). Only breakables with an ID remember that they were broken. Use a different ID for every breakable in the game.")]
		public string SaveId;

		protected bool _broken;
		protected Collider2D _collider2D;
		protected Renderer _renderer;
		protected SpriteRenderer _spriteRenderer;

		protected virtual void Awake()
		{
			_collider2D = GetComponent<Collider2D>();
			_renderer = GetComponent<Renderer>();
			_spriteRenderer = GetComponent<SpriteRenderer>();

			if (WasBrokenInSave())
			{
				ApplyAlreadyBrokenState();
			}
		}

		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			TryBreakFromCollider(other);
		}

		protected virtual void OnTriggerStay2D(Collider2D other)
		{
			TryBreakFromCollider(other);
		}

		protected virtual void OnCollisionEnter2D(Collision2D collision)
		{
			TryBreakFromCollider(collision.collider);
		}

		protected virtual void TryBreakFromCollider(Collider2D other)
		{
			if (_broken || other == null || !IsPlayerOwnedDamage(other))
			{
				return;
			}

			Break();
		}

		protected virtual bool IsPlayerOwnedDamage(Collider2D sourceCollider)
		{
			DamageOnTouch damageOnTouch = sourceCollider.GetComponent<DamageOnTouch>();
			if (damageOnTouch == null)
			{
				damageOnTouch = sourceCollider.GetComponentInParent<DamageOnTouch>();
			}

			if (damageOnTouch == null)
			{
				return false;
			}

			GameObject owner = damageOnTouch.Owner;
			if (owner == null)
			{
				Weapon weapon = sourceCollider.GetComponentInParent<Weapon>();
				if (weapon != null && weapon.Owner != null)
				{
					owner = weapon.Owner.gameObject;
				}
			}

			if (owner == null)
			{
				return false;
			}

			Character ownerCharacter = owner.GetComponentInParent<Character>();
			return ownerCharacter != null && ownerCharacter.CharacterType == Character.CharacterTypes.Player;
		}

		public virtual void Break()
		{
			if (_broken)
			{
				return;
			}

			_broken = true;
			SaveBrokenState();
			SpawnBreakEffect();
			SpawnReward();
			DisableAfterBreak();
		}

		/// <summary>Hook for subclasses to spawn their reward (souls, ...). Empty by default.</summary>
		protected virtual void SpawnReward()
		{
			// Intentionally empty.
		}

		protected virtual void DisableAfterBreak()
		{
			if (_collider2D != null)
			{
				_collider2D.enabled = false;
			}
			if (_renderer != null)
			{
				_renderer.enabled = false;
			}

			if (DestroyOnBreak)
			{
				Destroy(gameObject, DestroyDelay);
				return;
			}

			if (DisableOnBreak)
			{
				gameObject.SetActive(false);
			}
		}

		protected virtual void SpawnBreakEffect()
		{
			if (BreakEffectPrefab != null)
			{
				Instantiate(BreakEffectPrefab, transform.position, Quaternion.identity);
				return;
			}

			SpawnFallbackBreakEffect();
		}

		protected virtual void SpawnFallbackBreakEffect()
		{
			if (_spriteRenderer == null || _spriteRenderer.sprite == null)
			{
				return;
			}

			GameObject effect = new GameObject($"{name}_BreakEffect");
			effect.transform.position = transform.position;
			effect.transform.localScale = transform.lossyScale;

			SpriteRenderer effectRenderer = effect.AddComponent<SpriteRenderer>();
			effectRenderer.sprite = _spriteRenderer.sprite;
			effectRenderer.color = new Color(1f, 0.9f, 0.55f, 0.85f);
			effectRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
			effectRenderer.sortingOrder = _spriteRenderer.sortingOrder + 1;

			effect.AddComponent<BreakableBurstEffect>();
		}

		protected virtual void ApplyAlreadyBrokenState()
		{
			_broken = true;
			gameObject.SetActive(false);
		}

		protected virtual bool WasBrokenInSave()
		{
			if (string.IsNullOrEmpty(SaveId))
			{
				return false;
			}

			string data = PlayerPrefs.GetString(BrokenBreakablesKey, "");
			if (string.IsNullOrEmpty(data))
			{
				return false;
			}

			string[] ids = data.Split(',');
			for (int i = 0; i < ids.Length; i++)
			{
				if (ids[i] == SaveId)
				{
					return true;
				}
			}

			return false;
		}

		protected virtual void SaveBrokenState()
		{
			if (string.IsNullOrEmpty(SaveId) || WasBrokenInSave())
			{
				return;
			}

			string data = PlayerPrefs.GetString(BrokenBreakablesKey, "");
			data = string.IsNullOrEmpty(data) ? SaveId : data + "," + SaveId;

			PlayerPrefs.SetString(BrokenBreakablesKey, data);
			PlayerPrefs.Save();
		}
	}
}
