using System.Collections;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Breakables
{
	/// <summary>
	/// Shared root for every breakable object (soul Kendi, stone, ...).
	/// Handles player-damage detection, hit-points, the hit flash, the break
	/// effect and the optional "stays broken across saves" persistence.
	/// Subclasses only add their own reward / animation behavior.
	/// </summary>
	public class BreakableObject : MonoBehaviour
	{
		/// <summary>
		/// PlayerPrefs key holding the comma-separated list of SaveIds of every
		/// breakable that has already been destroyed in the current save.
		/// Wiped by GameFlowManager.ClearSaveData() on New Game.
		/// </summary>
		public const string BrokenBreakablesKey = "Maskbound.BrokenBreakables";

		[Header("Health")]
		[Tooltip("How many hit points this breakable has. It breaks when this reaches 0.")]
		public int MaxHealth = 1;
		[Tooltip("Flash color shown on the sprite whenever it takes a hit that doesn't break it. Defaults to a warm gold so it is visible on both light and dark sprites.")]
		public Color HitFlashColor = new Color(1f, 0.85f, 0.35f);
		[Tooltip("Flash the sprite with HitFlashColor for this duration whenever it takes a hit that doesn't break it.")]
		public float HitFlashDuration = 0.15f;
		[Tooltip("Cooldown between damage applications from the same source, so a held attack doesn't drain it instantly.")]
		public float DamageCooldown = 0.15f;

		[Header("Break")]
		public GameObject BreakEffectPrefab;
		public bool DisableOnBreak = true;
		public bool DestroyOnBreak = false;
		public float DestroyDelay = 0f;

		[Header("Soul Reward")]
		[Tooltip("Pickup prefab spawned when this object breaks. Leave empty for no soul drop.")]
		public GameObject SoulPickupPrefab;
		[Tooltip("How many soul pickups to spawn when this object breaks.")]
		public int SoulPickupCount = 1;
		[Tooltip("Offset from the object's position where the soul pickups spawn (base position).")]
		public Vector2 PickupSpawnOffset = new Vector2(0f, 0.5f);
		[Tooltip("How far each pick-up may be scattered randomly from the base position, so the souls spread around the object instead of stacking in one spot.")]
		public float PickupSpreadRadius = 0.4f;

		[Header("Persistence")]
		[Tooltip("Unique ID for this breakable (e.g. \"forest_stone_1\"). Only breakables with an ID remember that they were broken. Use a different ID for every breakable in the game.")]
		public string SaveId;

		protected int _currentHealth;
		protected bool _broken;
		protected Collider2D _collider2D;
		protected Renderer _renderer;
		protected SpriteRenderer _spriteRenderer;
		protected float _lastDamageTime;
		protected Color _originalColor = Color.white;
		protected Coroutine _hitFlashRoutine;

		protected virtual void Awake()
		{
			_collider2D = GetComponent<Collider2D>();
			_renderer = GetComponent<Renderer>();
			_spriteRenderer = GetComponent<SpriteRenderer>();

			_currentHealth = MaxHealth;

			if (_spriteRenderer != null)
			{
				_originalColor = _spriteRenderer.color;
			}

			if (WasBrokenInSave())
			{
				ApplyAlreadyBrokenState();
			}
		}

		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			TryApplyDamageFromCollider(other);
		}

		protected virtual void OnTriggerStay2D(Collider2D other)
		{
			TryApplyDamageFromCollider(other);
		}

		protected virtual void OnCollisionEnter2D(Collision2D collision)
		{
			TryApplyDamageFromCollider(collision.collider);
		}

		protected virtual void TryApplyDamageFromCollider(Collider2D other)
		{
			if (_broken || other == null || !IsPlayerOwnedDamage(other))
			{
				return;
			}

			// Cooldown so one continuous hitbox doesn't drain all health instantly.
			if (Time.time - _lastDamageTime < DamageCooldown)
			{
				return;
			}
			_lastDamageTime = Time.time;

			int damage = GetDamageFromSource(other);
			TakeDamage(Mathf.Max(1, damage));
		}

		protected virtual int GetDamageFromSource(Collider2D sourceCollider)
		{
			DamageOnTouch damageOnTouch = sourceCollider.GetComponent<DamageOnTouch>();
			if (damageOnTouch == null)
			{
				damageOnTouch = sourceCollider.GetComponentInParent<DamageOnTouch>();
			}

			if (damageOnTouch == null)
			{
				return 1;
			}

			float min = damageOnTouch.MinDamageCaused;
			float max = Mathf.Max(damageOnTouch.MaxDamageCaused, min);
			return Mathf.Max(1, Mathf.RoundToInt(Random.Range(min, max)));
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

		/// <summary>
		/// Applies hit points to the breakable. Flashes on hit while still alive and
		/// breaks the object once health reaches zero.
		/// </summary>
		public virtual void TakeDamage(int hitPoints)
		{
			if (_broken || hitPoints <= 0)
			{
				return;
			}

			_currentHealth -= hitPoints;

			if (_currentHealth > 0)
			{
				PlayHitFlash();
				return;
			}

			Break();
		}

		protected virtual void PlayHitFlash()
		{
			if (_spriteRenderer == null)
			{
				return;
			}

			if (_hitFlashRoutine != null)
			{
				StopCoroutine(_hitFlashRoutine);
			}
			_hitFlashRoutine = StartCoroutine(HitFlashRoutine());
		}

		protected virtual IEnumerator HitFlashRoutine()
		{
			_spriteRenderer.color = HitFlashColor;

			if (HitFlashDuration > 0f)
			{
				yield return new WaitForSeconds(HitFlashDuration);
			}

			_spriteRenderer.color = _originalColor;
			_hitFlashRoutine = null;
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

		/// <summary>Hook for subclasses to spawn their reward. Spawns the configured soul pickups by default.</summary>
		protected virtual void SpawnReward()
		{
			SpawnSoulPickups();
		}

		/// <summary>
		/// Spawns the configured soul pickups at this object's position, fanning them out
		/// slightly when there is more than one so they don't stack on top of each other.
		/// </summary>
		protected virtual void SpawnSoulPickups()
		{
			if (SoulPickupPrefab == null)
			{
				return;
			}

			int count = Mathf.Max(0, SoulPickupCount);
			Vector2 basePos = (Vector2)transform.position + PickupSpawnOffset;
			for (int i = 0; i < count; i++)
			{
				Vector2 scatter = Random.insideUnitCircle * PickupSpreadRadius;
				Vector3 spawnPos = basePos + scatter;
				spawnPos.z = transform.position.z;

				Instantiate(SoulPickupPrefab, spawnPos, Quaternion.identity);
			}
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
