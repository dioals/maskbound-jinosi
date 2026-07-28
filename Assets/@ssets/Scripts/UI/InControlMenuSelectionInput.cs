using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/InControl Menu Selection Input")]
	public class InControlMenuSelectionInput : MonoBehaviour
	{
		[Header("References")]
		public GameObject PanelRoot;
		public Selectable FirstSelected;

		[Header("Controller")]
		public InputControlType SubmitButton = InputControlType.Action1;
		public InputControlType CancelButton = InputControlType.Action2;
		public bool UseLeftStick = true;
		public bool UseDPad = true;
		[Range(0.1f, 1f)] public float StickThreshold = 0.5f;

		[Header("Repeat")]
		public float InitialRepeatDelay = 0.35f;
		public float RepeatRate = 0.12f;

		private readonly List<Selectable> _selectables = new List<Selectable>();
		private float _nextMoveTime;
		private int _lastMoveDirection;

		protected virtual void Reset()
		{
			PanelRoot = gameObject;
			FirstSelected = GetComponentInChildren<Selectable>(true);
		}

		protected virtual void OnEnable()
		{
			SelectDefault();
		}

		protected virtual void Update()
		{
			if (!IsPanelActive())
			{
				return;
			}

			EnsureValidSelection();
			HandleMove();
			HandleSubmit();
			HandleCancel();
		}

		public virtual void SelectDefault()
		{
			if (!IsPanelActive())
			{
				return;
			}

			RefreshSelectables();
			Selectable target = FirstSelected != null && IsUsableSelectable(FirstSelected)
				? FirstSelected
				: (_selectables.Count > 0 ? _selectables[0] : null);

			if (target != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(target.gameObject);
			}
		}

		private void EnsureValidSelection()
		{
			if (EventSystem.current == null)
			{
				return;
			}

			GameObject selected = EventSystem.current.currentSelectedGameObject;
			if (selected == null || !selected.activeInHierarchy || selected.GetComponent<Selectable>() == null)
			{
				SelectDefault();
			}
		}

		private void HandleMove()
		{
			int direction = GetMoveDirection();
			if (direction == 0)
			{
				_lastMoveDirection = 0;
				_nextMoveTime = 0f;
				return;
			}

			if (_lastMoveDirection == direction && Time.unscaledTime < _nextMoveTime)
			{
				return;
			}

			MoveSelection(direction);
			_lastMoveDirection = direction;
			_nextMoveTime = Time.unscaledTime + (_nextMoveTime <= 0f ? InitialRepeatDelay : RepeatRate);
		}

		private void MoveSelection(int direction)
		{
			RefreshSelectables();
			if (_selectables.Count == 0 || EventSystem.current == null)
			{
				return;
			}

			Selectable current = EventSystem.current.currentSelectedGameObject != null
				? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
				: null;

			int currentIndex = current != null ? _selectables.IndexOf(current) : -1;
			int nextIndex = currentIndex < 0 ? 0 : currentIndex + direction;
			nextIndex = ((nextIndex % _selectables.Count) + _selectables.Count) % _selectables.Count;

			EventSystem.current.SetSelectedGameObject(_selectables[nextIndex].gameObject);
		}

		private void HandleSubmit()
		{
			InputDevice device = InputManager.ActiveDevice;
			if (device == null || !device.GetControl(SubmitButton).WasPressed || EventSystem.current == null)
			{
				return;
			}

			GameObject selected = EventSystem.current.currentSelectedGameObject;
			if (selected != null)
			{
				ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
			}
		}

		private void HandleCancel()
		{
			InputDevice device = InputManager.ActiveDevice;
			if (device == null || !device.GetControl(CancelButton).WasPressed || EventSystem.current == null)
			{
				return;
			}

			GameObject selected = EventSystem.current.currentSelectedGameObject;
			if (selected != null)
			{
				ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.cancelHandler);
			}
		}

		private int GetMoveDirection()
		{
			InputDevice device = InputManager.ActiveDevice;
			if (device == null)
			{
				return 0;
			}

			if (UseDPad)
			{
				if (device.DPadDown.IsPressed)
				{
					return 1;
				}

				if (device.DPadUp.IsPressed)
				{
					return -1;
				}
			}

			if (UseLeftStick)
			{
				float y = device.LeftStickY.Value;
				if (y <= -StickThreshold)
				{
					return 1;
				}

				if (y >= StickThreshold)
				{
					return -1;
				}
			}

			return 0;
		}

		private void RefreshSelectables()
		{
			_selectables.Clear();
			GameObject root = PanelRoot != null ? PanelRoot : gameObject;
			Selectable[] children = root.GetComponentsInChildren<Selectable>(false);

			for (int i = 0; i < children.Length; i++)
			{
				if (IsUsableSelectable(children[i]))
				{
					_selectables.Add(children[i]);
				}
			}
		}

		private bool IsUsableSelectable(Selectable selectable)
		{
			return selectable != null
				&& selectable.gameObject.activeInHierarchy
				&& selectable.IsInteractable();
		}

		private bool IsPanelActive()
		{
			GameObject root = PanelRoot != null ? PanelRoot : gameObject;
			return root.activeInHierarchy;
		}
	}
}
