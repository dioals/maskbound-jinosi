using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Breakables
{
	[AddComponentMenu("Maskbound/World/Breakable Soul Object")]
	public class BreakableSoulObject : MonoBehaviour
	{
		[Header("Break")]
		public GameObject BreakEffectPrefab;
		public bool DisableOnBreak = true;
		public bool DestroyOnBreak = false;
		public float DestroyDelay = 0f;

		[Header("Reward")]
		public GameObject SoulPickupPrefab;
		public int SoulPickupCount = 1;
		public Vector2 PickupSpawnOffset = new Vector2(0f, 0.5f);

		protected bool _broken;
		protected Collider2D _collider2D;
		protected Renderer _renderer;
		protected SpriteRenderer _spriteRenderer;

		protected virtual void Awake()
		{
			_collider2D = GetComponent<Collider2D>();
			_renderer = GetComponent<Renderer>();
			_spriteRenderer = GetComponent<SpriteRenderer>();
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
			if (_broken || other == null)
			{
				return;
			}

			DamageOnTouch damageOnTouch = other.GetComponent<DamageOnTouch>();
			if (damageOnTouch == null)
			{
				damageOnTouch = other.GetComponentInParent<DamageOnTouch>();
			}

			if (damageOnTouch == null || !IsPlayerOwnedDamage(damageOnTouch, other))
			{
				return;
			}

			Break();
		}

		protected virtual bool IsPlayerOwnedDamage(DamageOnTouch damageOnTouch, Collider2D sourceCollider)
		{
			GameObject owner = damageOnTouch.Owner;
			if (owner == null && sourceCollider != null)
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
			SpawnBreakEffect();
			SpawnSoulPickups();

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

		protected virtual void SpawnSoulPickups()
		{
			if (SoulPickupPrefab == null)
			{
				return;
			}

			int count = Mathf.Max(0, SoulPickupCount);
			for (int i = 0; i < count; i++)
			{
				Vector3 offset = new Vector3(PickupSpawnOffset.x, PickupSpawnOffset.y, 0f);
				if (count > 1)
				{
					offset.x += (i - ((count - 1) * 0.5f)) * 0.35f;
				}

				Instantiate(SoulPickupPrefab, transform.position + offset, Quaternion.identity);
			}
		}
	}
}
