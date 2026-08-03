using System.Collections.Generic;
using InControl;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Main Menu Controller")]
	public class MainMenuController : MonoBehaviour
	{
		private struct HistoryEntry
		{
			public MainMenuPage Page;
			public Selectable ReturnSelection;
		}

		public MainMenuPage HomePage;
		public MainMenuPage[] Pages;
		public TMP_Text PageTitleText;
		public InputControlType ControllerBackButton = InputControlType.Action2;
		public KeyCode KeyboardBackKey = KeyCode.Escape;

		private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
		private MainMenuPage _currentPage;

		protected virtual void OnEnable()
		{
			GoHome();
		}

		protected virtual void Update()
		{
			InputDevice device = InputManager.ActiveDevice;
			bool controllerBack = device != null && device.GetControl(ControllerBackButton).WasPressed;
			if (UnityEngine.Input.GetKeyDown(KeyboardBackKey) || controllerBack)
			{
				Back();
			}
		}

		public virtual void OpenPage(MainMenuPage page, Selectable returnSelection = null)
		{
			if (page == null || page == _currentPage) return;
			if (_currentPage != null)
			{
				_history.Add(new HistoryEntry { Page = _currentPage, ReturnSelection = returnSelection });
			}

			ShowOnly(page);
		}

		public virtual void Back()
		{
			if (_history.Count == 0)
			{
				GoHome();
				return;
			}

			int index = _history.Count - 1;
			HistoryEntry entry = _history[index];
			_history.RemoveAt(index);
			ShowOnly(entry.Page, entry.ReturnSelection);
		}

		public virtual void GoHome()
		{
			_history.Clear();
			ShowOnly(HomePage);
		}

		private void ShowOnly(MainMenuPage page, Selectable preferred = null)
		{
			if (page == null) return;
			foreach (MainMenuPage candidate in Pages)
			{
				if (candidate != null) candidate.gameObject.SetActive(candidate == page);
			}

			_currentPage = page;
			page.gameObject.SetActive(true);
			if (PageTitleText != null) PageTitleText.text = page.Title;

			Selectable selected = preferred != null && preferred.gameObject.activeInHierarchy
				? preferred
				: page.ResolveFirstSelected();
			if (selected != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(null);
				EventSystem.current.SetSelectedGameObject(selected.gameObject);
			}
		}
	}
}
