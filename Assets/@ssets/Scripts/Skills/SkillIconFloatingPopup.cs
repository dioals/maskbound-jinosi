using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Skill Icon Floating Popup")]
	[RequireComponent(typeof(SpriteRenderer))]
	public class SkillIconFloatingPopup : MonoBehaviour
	{
		[Header("Tween")]
		[Min(0f)] public float FloatDistance = 0.75f;
		[Min(0.01f)] public float Duration = 0.6f;

		protected SpriteRenderer _sprite;
		protected Vector3 _startPosition;
		protected Color _startColor;
		protected float _elapsed;

		protected virtual void Awake()
		{
			_sprite = GetComponent<SpriteRenderer>();
			_startPosition = transform.position;
			_startColor = _sprite.color;
		}

		/// <summary>
		/// Overrides the icon shown while this floating popup is alive.
		/// </summary>
		public virtual void SetIcon(Sprite icon)
		{
			if (_sprite == null)
			{
				_sprite = GetComponent<SpriteRenderer>();
			}

			if (_sprite != null)
			{
				_sprite.sprite = icon;
			}
		}

		protected virtual void Update()
		{
			_elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(_elapsed / Duration);

			transform.position = _startPosition + Vector3.up * (FloatDistance * t);

			Color color = _startColor;
			color.a = _startColor.a * (1f - t);
			_sprite.color = color;

			if (t >= 1f)
			{
				Destroy(gameObject);
			}
		}
	}
}
