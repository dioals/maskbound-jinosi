using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	public class LivesUIBinder : MonoBehaviour, MMEventListener<CorgiEngineEvent>
	{
		public enum EmptyLifeDisplayMode
		{
			Hide,
			ReplaceSprite
		}

		[SerializeField] private Image[] lifeIcons;
		[SerializeField] private EmptyLifeDisplayMode displayMode = EmptyLifeDisplayMode.Hide;
		[SerializeField] private Sprite fullLifeSprite;
		[SerializeField] private Sprite emptyLifeSprite;
		private Sprite[] _initialSprites;

		private void Awake()
		{
			if (lifeIcons == null)
			{
				return;
			}

			_initialSprites = new Sprite[lifeIcons.Length];
			for (int i = 0; i < lifeIcons.Length; i++)
			{
				_initialSprites[i] = lifeIcons[i] != null ? lifeIcons[i].sprite : null;
			}
		}

		private void Start()
		{
			Refresh();
		}

		private void OnEnable()
		{
			this.MMEventStartListening<CorgiEngineEvent>();
			Refresh();
		}

		private void OnDisable()
		{
			this.MMEventStopListening<CorgiEngineEvent>();
		}

		public void OnMMEvent(CorgiEngineEvent engineEvent)
		{
			if (engineEvent.EventType == CorgiEngineEventTypes.LivesCountChanged
				|| engineEvent.EventType == CorgiEngineEventTypes.LevelStart)
			{
				Refresh();
			}
		}

		public void Refresh()
		{
			if (!GameManager.HasInstance || lifeIcons == null)
			{
				return;
			}

			int currentLives = Mathf.Max(0, GameManager.Instance.CurrentLives);

			for (int i = 0; i < lifeIcons.Length; i++)
			{
				Image icon = lifeIcons[i];
				if (icon == null)
				{
					continue;
				}

				bool isFull = i < currentLives;
				if (displayMode == EmptyLifeDisplayMode.Hide)
				{
					icon.gameObject.SetActive(isFull);
					continue;
				}

				icon.gameObject.SetActive(true);
				if (isFull)
				{
					icon.sprite = fullLifeSprite != null ? fullLifeSprite : _initialSprites[i];
				}
				else if (!isFull && emptyLifeSprite != null)
				{
					icon.sprite = emptyLifeSprite;
				}
			}
		}
	}
}
