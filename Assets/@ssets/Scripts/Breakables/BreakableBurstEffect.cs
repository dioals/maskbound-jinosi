using UnityEngine;

namespace MaskboundJinosi.Breakables
{
	[AddComponentMenu("")]
	public class BreakableBurstEffect : MonoBehaviour
	{
		public float Duration = 0.18f;
		public float ScaleMultiplier = 1.35f;

		protected SpriteRenderer _spriteRenderer;
		protected Vector3 _startScale;
		protected float _time;

		protected virtual void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
			_startScale = transform.localScale;
		}

		protected virtual void Update()
		{
			_time += Time.deltaTime;
			float progress = Duration <= 0f ? 1f : Mathf.Clamp01(_time / Duration);

			transform.localScale = Vector3.Lerp(_startScale, _startScale * ScaleMultiplier, progress);

			if (_spriteRenderer != null)
			{
				Color color = _spriteRenderer.color;
				color.a = Mathf.Lerp(0.85f, 0f, progress);
				_spriteRenderer.color = color;
			}

			if (progress >= 1f)
			{
				Destroy(gameObject);
			}
		}
	}
}
