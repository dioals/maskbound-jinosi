using TMPro;
using UnityEngine;

namespace MaskboundJinosi.Combat
{
	[AddComponentMenu("Maskbound/Combat/Damage Number Popup")]
	[RequireComponent(typeof(TextMeshPro))]
	public class DamageNumberPopup : MonoBehaviour
	{
		[Header("Tween")]
		[Min(0.01f)] public float Duration = 0.8f;
		public Vector2 RiseHeightRange = new Vector2(0.6f, 1.2f);
		public Vector2 HorizontalDistanceRange = new Vector2(0.2f, 0.6f);
		public AnimationCurve FadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

		protected TextMeshPro _text;
		protected Vector3 _startPosition;
		protected Color _startColor;
		protected float _elapsed;
		protected float _riseHeight;
		protected float _horizontalDistance;

		protected virtual void Awake()
		{
			_text = GetComponent<TextMeshPro>();
			_startPosition = transform.position;
			_startColor = _text.color;
			_riseHeight = Random.Range(RiseHeightRange.x, RiseHeightRange.y);
			_horizontalDistance = Random.Range(HorizontalDistanceRange.x, HorizontalDistanceRange.y);
		}

		public virtual void Initialize(int damage, float horizontalDirection)
		{
			_text.text = damage.ToString();

			float sign = horizontalDirection != 0f ? Mathf.Sign(horizontalDirection) : (Random.value < 0.5f ? -1f : 1f);
			_horizontalDistance *= sign;
		}

		protected virtual void Update()
		{
			_elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(_elapsed / Duration);

			float height = 4f * _riseHeight * t * (1f - t);
			float horizontal = _horizontalDistance * t;
			transform.position = _startPosition + new Vector3(horizontal, height, 0f);

			Color color = _startColor;
			color.a = _startColor.a * FadeCurve.Evaluate(t);
			_text.color = color;

			if (t >= 1f)
			{
				Destroy(gameObject);
			}
		}
	}
}
