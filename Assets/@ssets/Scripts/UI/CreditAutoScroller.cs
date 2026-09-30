using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	/// <summary>
	/// Auto-scroll ScrollRect vertikal ala movie credit: naik sampai habis, jeda,
	/// turun balik ke atas, jeda, ulangi terus. Kalau user drag / scroll wheel,
	/// auto-scroll berhenti sebentar lalu lanjut dari posisi terakhir.
	/// Pasang di objek yang sama dengan ScrollRect.
	/// </summary>
	[AddComponentMenu("Maskbound/UI/Credit Auto Scroller")]
	[RequireComponent(typeof(ScrollRect))]
	public class CreditAutoScroller : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
	{
		[Header("Speed")]
		[Tooltip("Kecepatan scroll turun ke bawah daftar (pixel per detik, unscaled time).")]
		public float ScrollSpeed = 60f;
		[Tooltip("Kecepatan balik ke atas. Kalau 0 pakai ScrollSpeed.")]
		public float ReturnSpeed = 180f;

		[Header("Pause")]
		[Tooltip("Jeda di posisi paling atas sebelum mulai scroll (detik).")]
		public float PauseAtTop = 2f;
		[Tooltip("Jeda di posisi paling bawah sebelum balik ke atas (detik).")]
		public float PauseAtBottom = 2f;
		[Tooltip("Jeda setelah user selesai scroll manual sebelum auto-scroll lanjut (detik).")]
		public float ResumeDelayAfterInput = 3f;

		[Header("Behaviour")]
		[Tooltip("Reset ke paling atas setiap kali halaman credit dibuka.")]
		public bool ResetOnEnable = true;

		private ScrollRect _scrollRect;
		private bool _movingDown = true;
		private float _waitUntil;
		private bool _dragging;

		protected virtual void Awake()
		{
			_scrollRect = GetComponent<ScrollRect>();
		}

		protected virtual void OnEnable()
		{
			if (_scrollRect == null)
			{
				_scrollRect = GetComponent<ScrollRect>();
			}

			if (ResetOnEnable)
			{
				_scrollRect.StopMovement();
				_scrollRect.verticalNormalizedPosition = 1f;
				_movingDown = true;
			}

			_dragging = false;
			_waitUntil = Time.unscaledTime + PauseAtTop;
		}

		protected virtual void LateUpdate()
		{
			if (_scrollRect == null || _scrollRect.content == null || _dragging)
			{
				return;
			}

			if (Time.unscaledTime < _waitUntil)
			{
				return;
			}

			float scrollable = GetScrollableHeight();
			if (scrollable <= 0.5f)
			{
				// Credit muat di window, tidak perlu scroll.
				return;
			}

			float speed = _movingDown ? ScrollSpeed : (ReturnSpeed > 0f ? ReturnSpeed : ScrollSpeed);
			float step = speed * Time.unscaledDeltaTime / scrollable;
			float position = _scrollRect.verticalNormalizedPosition;

			if (_movingDown)
			{
				position -= step;
				if (position <= 0f)
				{
					position = 0f;
					_movingDown = false;
					_waitUntil = Time.unscaledTime + PauseAtBottom;
				}
			}
			else
			{
				position += step;
				if (position >= 1f)
				{
					position = 1f;
					_movingDown = true;
					_waitUntil = Time.unscaledTime + PauseAtTop;
				}
			}

			_scrollRect.velocity = Vector2.zero;
			_scrollRect.verticalNormalizedPosition = position;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			_dragging = true;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			_dragging = false;
			PauseAfterInput();
		}

		public void OnScroll(PointerEventData eventData)
		{
			PauseAfterInput();
		}

		private void PauseAfterInput()
		{
			_waitUntil = Time.unscaledTime + ResumeDelayAfterInput;

			// Lanjut ke arah ujung yang lebih jauh supaya tidak langsung mentok.
			_movingDown = _scrollRect.verticalNormalizedPosition > 0.5f;
		}

		private float GetScrollableHeight()
		{
			RectTransform viewport = _scrollRect.viewport != null
				? _scrollRect.viewport
				: (RectTransform)_scrollRect.transform;

			return _scrollRect.content.rect.height - viewport.rect.height;
		}
	}
}
