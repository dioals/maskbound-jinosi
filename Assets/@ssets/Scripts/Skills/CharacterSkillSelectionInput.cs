using InControl;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Character Skill Selection Input")]
	public class CharacterSkillSelectionInput : MonoBehaviour
	{
		[Header("References")]
		public CharacterSkillCaster SkillCaster;

		[Header("Keyboard")]
		public bool EnableKeyboard = true;
		public KeyCode PreviousSkillKey = KeyCode.LeftBracket;
		public KeyCode NextSkillKey = KeyCode.RightBracket;
		public KeyCode ActivateSelectedSkillKey = KeyCode.Q;

		[Header("Controller")]
		public bool EnableController = true;
		public InputControlType PreviousSkillButton = InputControlType.LeftBumper;
		public InputControlType NextSkillButton = InputControlType.RightBumper;
		public InputControlType ActivateSelectedSkillButton = InputControlType.LeftTrigger;

		[Header("Selection")]
		public bool WrapAround = true;
		public bool ActivateOnSelect = false;

		[Header("Runtime")]
		[SerializeField] private int selectedSlotIndex;
		[SerializeField] private string selectedSkillName;

		protected virtual void Awake()
		{
			if (SkillCaster == null)
			{
				SkillCaster = GetComponentInParent<CharacterSkillCaster>();
			}

			RefreshRuntimeSelection();
		}

		protected virtual void Update()
		{
			if (SkillCaster == null)
			{
				return;
			}

			if (WasPreviousPressed())
			{
				SelectRelative(-1);
			}

			if (WasNextPressed())
			{
				SelectRelative(1);
			}

			if (WasActivatePressed())
			{
				SkillCaster.ActivateSelectedSkill();
			}

			RefreshRuntimeSelection();
		}

		public virtual void SelectRelative(int direction)
		{
			if (SkillCaster == null || SkillCaster.SkillSlots == null || SkillCaster.SkillSlots.SlotCount <= 0)
			{
				return;
			}

			int slotCount = SkillCaster.SkillSlots.SlotCount;
			int nextIndex = SkillCaster.SelectedSkillSlotIndex + direction;

			if (WrapAround)
			{
				nextIndex = ((nextIndex % slotCount) + slotCount) % slotCount;
			}
			else
			{
				nextIndex = Mathf.Clamp(nextIndex, 0, slotCount - 1);
			}

			SkillCaster.SelectSkillSlot(nextIndex);

			if (ActivateOnSelect)
			{
				SkillCaster.ActivateSelectedSkill();
			}
		}

		public virtual void SelectSlot(int slotIndex)
		{
			if (SkillCaster == null)
			{
				return;
			}

			SkillCaster.SelectSkillSlot(slotIndex);

			if (ActivateOnSelect)
			{
				SkillCaster.ActivateSelectedSkill();
			}
		}

		private bool WasPreviousPressed()
		{
			if (EnableKeyboard && UnityEngine.Input.GetKeyDown(PreviousSkillKey))
			{
				return true;
			}

			return EnableController && WasControllerButtonPressed(PreviousSkillButton);
		}

		private bool WasNextPressed()
		{
			if (EnableKeyboard && UnityEngine.Input.GetKeyDown(NextSkillKey))
			{
				return true;
			}

			return EnableController && WasControllerButtonPressed(NextSkillButton);
		}

		private bool WasActivatePressed()
		{
			if (EnableKeyboard && UnityEngine.Input.GetKeyDown(ActivateSelectedSkillKey))
			{
				return true;
			}

			return EnableController && WasControllerButtonPressed(ActivateSelectedSkillButton);
		}

		private bool WasControllerButtonPressed(InputControlType inputControl)
		{
			InputDevice device = InputManager.ActiveDevice;
			return device != null && device.GetControl(inputControl).WasPressed;
		}

		private void RefreshRuntimeSelection()
		{
			if (SkillCaster == null)
			{
				selectedSlotIndex = 0;
				selectedSkillName = "";
				return;
			}

			selectedSlotIndex = SkillCaster.SelectedSkillSlotIndex;
			Skill skill = SkillCaster.SkillSlots != null ? SkillCaster.SkillSlots.GetSkill(selectedSlotIndex) : null;
			selectedSkillName = skill != null ? skill.DisplayName : "(empty)";
		}
	}
}
