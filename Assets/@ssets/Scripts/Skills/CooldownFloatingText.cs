using TMPro;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Cooldown Floating Text")]
	[RequireComponent(typeof(TextMeshPro))]
	public class CooldownFloatingText : MonoBehaviour
	{
		[Header("Tween")]
		[Min(0f)] public float FloatDistance = 0.75f;
		[Min(0.01f)] public float Duration = 0.6f;

		protected TextMeshPro _text;
		protected Vector3 _startPosition;
		protected Color _startColor;
		protected float _elapsed;

		protected virtual void Awake()
		{
			_text = GetComponent<TextMeshPro>();
			_startPosition = transform.position;
			_startColor = _text.color;
		}

		protected virtual void Update()
		{
			_elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(_elapsed / Duration);

			transform.position = _startPosition + Vector3.up * (FloatDistance * t);

			Color color = _startColor;
			color.a = _startColor.a * (1f - t);
			_text.color = color;

			if (t >= 1f)
			{
				Destroy(gameObject);
			}
		}
	}
}
