using System.Collections.Generic;
using InControl;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskboundJinosi.Debugging
{
	[AddComponentMenu("Maskbound/Debug/Mod Menu Controller")]
	public class DevModMenuController : MonoBehaviour
	{
		private struct NavigationEntry
		{
			public DevModMenuPage Page;
			public Selectable ReturnSelection;
		}

		[Header("References")]
		public GameObject MenuRoot;
		public DevModMenuPage HomePage;
		public DevModMenuPage[] Pages;
		public TMP_Text TitleText;
		public TMP_Text BreadcrumbText;

		[Header("Open / Close")]
		public bool StartVisible;
		public bool PauseWhileOpen = true;
		public bool ManageCursor = true;
		public KeyCode KeyboardToggleKey = KeyCode.F1;
		public KeyCode KeyboardBackKey = KeyCode.Backspace;
		public bool EnableControllerToggle;
		public InputControlType ControllerToggleButton = InputControlType.Command;
		public InputControlType ControllerBackButton = InputControlType.Action2;

		private readonly List<NavigationEntry> _history = new List<NavigationEntry>();
		private DevModMenuPage _currentPage;
		private float _timeScaleBeforeOpen = 1f;
		private CursorLockMode _cursorLockBeforeOpen;
		private bool _cursorVisibleBeforeOpen;
		private bool _isOpen;

		public bool IsOpen => _isOpen;
		public DevModMenuPage CurrentPage => _currentPage;

		protected virtual void Start()
		{
			if (MenuRoot == null)
			{
				MenuRoot = gameObject;
			}

			SetMenuVisible(StartVisible);
		}

		protected virtual void Update()
		{
			if (UnityEngine.Input.GetKeyDown(KeyboardToggleKey)
				|| (EnableControllerToggle && ControllerButtonPressed(ControllerToggleButton)))
			{
				ToggleMenu();
				return;
			}

			if (!_isOpen)
			{
				return;
			}

			if (UnityEngine.Input.GetKeyDown(KeyboardBackKey)
				|| UnityEngine.Input.GetKeyDown(KeyCode.Escape)
				|| ControllerButtonPressed(ControllerBackButton))
			{
				Back();
			}
		}

		public virtual void ToggleMenu()
		{
			SetMenuVisible(!_isOpen);
		}

		public virtual void OpenMenu()
		{
			SetMenuVisible(true);
		}

		public virtual void CloseMenu()
		{
			SetMenuVisible(false);
		}

		public virtual void OpenPage(DevModMenuPage page, Selectable returnSelection = null)
		{
			if (page == null || page == _currentPage)
			{
				return;
			}

			if (_currentPage != null)
			{
				_history.Add(new NavigationEntry
				{
					Page = _currentPage,
					ReturnSelection = returnSelection
				});
			}

			ShowOnly(page);
		}

		public virtual void Back()
		{
			if (_history.Count == 0)
			{
				CloseMenu();
				return;
			}

			int lastIndex = _history.Count - 1;
			NavigationEntry previous = _history[lastIndex];
			_history.RemoveAt(lastIndex);
			ShowOnly(previous.Page, previous.ReturnSelection);
		}

		public virtual void GoHome()
		{
			_history.Clear();
			ShowOnly(HomePage);
		}

		private void SetMenuVisible(bool visible)
		{
			if (MenuRoot == null)
			{
				return;
			}

			if (visible == _isOpen && MenuRoot.activeSelf == visible)
			{
				return;
			}

			_isOpen = visible;
			if (visible)
			{
				_timeScaleBeforeOpen = Time.timeScale;
				_cursorLockBeforeOpen = Cursor.lockState;
				_cursorVisibleBeforeOpen = Cursor.visible;
				MenuRoot.SetActive(true);
				_history.Clear();
				ShowOnly(HomePage);

				if (PauseWhileOpen)
				{
					Time.timeScale = 0f;
				}

				if (ManageCursor)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
			}
			else
			{
				MenuRoot.SetActive(false);
				if (PauseWhileOpen)
				{
					Time.timeScale = _timeScaleBeforeOpen;
				}

				if (ManageCursor)
				{
					Cursor.lockState = _cursorLockBeforeOpen;
					Cursor.visible = _cursorVisibleBeforeOpen;
				}
			}
		}

		private void ShowOnly(DevModMenuPage page, Selectable preferredSelection = null)
		{
			if (page == null)
			{
				return;
			}

			foreach (DevModMenuPage candidate in Pages)
			{
				if (candidate != null)
				{
					candidate.gameObject.SetActive(candidate == page);
				}
			}

			_currentPage = page;
			_currentPage.gameObject.SetActive(true);
			RefreshHeader();

			Selectable selection = preferredSelection != null && preferredSelection.gameObject.activeInHierarchy
				? preferredSelection
				: page.ResolveFirstSelected();

			if (selection != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(null);
				EventSystem.current.SetSelectedGameObject(selection.gameObject);
			}
		}

		private void RefreshHeader()
		{
			if (TitleText != null)
			{
				TitleText.text = _currentPage != null ? _currentPage.Title : string.Empty;
			}

			if (BreadcrumbText == null)
			{
				return;
			}

			List<string> titles = new List<string>();
			foreach (NavigationEntry entry in _history)
			{
				if (entry.Page != null)
				{
					titles.Add(entry.Page.Title);
				}
			}

			if (_currentPage != null)
			{
				titles.Add(_currentPage.Title);
			}

			BreadcrumbText.text = string.Join(" > ", titles);
		}

		private static bool ControllerButtonPressed(InputControlType controlType)
		{
			InputDevice device = InputManager.ActiveDevice;
			return device != null && device.GetControl(controlType).WasPressed;
		}

		protected virtual void OnDisable()
		{
			if (_isOpen && PauseWhileOpen)
			{
				Time.timeScale = _timeScaleBeforeOpen;
			}
		}
	}
}
