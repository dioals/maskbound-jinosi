using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	/// <summary>
	/// Lets a jump-flavoured active skill (ActiveSkillData.ActivateWithJumpButton, e.g. Jejak Sukma)
	/// be cast with the Jump button instead of the generic activate-skill button (L2 / Q), so a double
	/// jump feels like a jump rather than a skill cast.
	///
	/// This runs in LateUpdate on purpose: Corgi drives CharacterJump.HandleInput from Character.Update,
	/// so by the time we look at the jump button the normal jump has already had its turn. We only react
	/// to presses CharacterJump silently refused because the player is out of jumps mid-air.
	///
	/// The skill is gated per airborne stretch, not by its cooldown: one cast per time in the air, and it
	/// comes back the moment the character touches ground.
	/// </summary>
	[AddComponentMenu("Maskbound/Skills/Character Jump Skill Input")]
	public class CharacterJumpSkillInput : MonoBehaviour
	{
		[Header("References")]
		public CharacterSkillCaster SkillCaster;

		[Header("Runtime")]
		public bool LogDebug;
		[SerializeField] private bool usedSinceGrounded;

		protected Character _character;
		protected CharacterJump _jump;
		protected CorgiController _controller;

		protected virtual void Awake()
		{
			if (SkillCaster == null)
			{
				SkillCaster = GetComponentInParent<CharacterSkillCaster>();
			}

			_character = GetComponentInParent<Character>();
			if (_character != null)
			{
				_jump = _character.FindAbility<CharacterJump>();
				_controller = _character.GetComponentInParent<CorgiController>();
			}
		}

		protected virtual void LateUpdate()
		{
			if (_controller == null || _jump == null || _character == null || SkillCaster == null)
			{
				return;
			}

			// Touching ground is the only thing that recharges the skill jump - no cooldown involved.
			if (_controller.State.IsGrounded)
			{
				usedSinceGrounded = false;
			}

			if (!JumpButtonWentDownThisFrame())
			{
				return;
			}

			// Jump ability disabled by progression (PlayerControlToggles) or otherwise blocked
			if (!_jump.enabled || !_jump.AbilityAuthorized)
			{
				return;
			}

			// A regular, coyote or buffered jump already consumed this press
			if (_jump.JumpHappenedThisFrame)
			{
				return;
			}

			// Only step in where CharacterJump refused: airborne with nothing left.
			// NumberOfJumpsLeft > 0 also covers the coyote window, which stays a normal jump.
			if (_controller.State.IsGrounded || _jump.NumberOfJumpsLeft > 0)
			{
				return;
			}

			if (usedSinceGrounded)
			{
				return;
			}

			int slotIndex = FindJumpButtonSkillSlot();
			if (slotIndex < 0)
			{
				return;
			}

			// Goes through the slot manager rather than CharacterSkillCaster.ActivateSkillSlot so a
			// refused cast doesn't pop the cooldown feedback on every mid-air jump tap.
			if (SkillCaster.SkillSlots.ActivateSkillInSlot(slotIndex))
			{
				usedSinceGrounded = true;

				if (LogDebug)
				{
					Debug.Log($"Jump button cast skill in slot {slotIndex}", this);
				}
			}
		}

		/// <summary>
		/// Reads the same JumpButton the CharacterJump ability reads, so the skill follows whatever the
		/// player has Jump bound to (MaskboundInputBindings) instead of a second, duplicated binding.
		/// </summary>
		protected virtual bool JumpButtonWentDownThisFrame()
		{
			InputManager inputManager = _character.LinkedInputManager;
			return (inputManager != null)
			       && (inputManager.JumpButton != null)
			       && (inputManager.JumpButton.State.CurrentState == MMInput.ButtonStates.ButtonDown);
		}

		/// <summary>
		/// Finds the first equipped active skill flagged as jump-button activated. Returns -1 when no such
		/// skill is equipped, which is what keeps this whole path inert for players without it.
		/// </summary>
		protected virtual int FindJumpButtonSkillSlot()
		{
			SkillSlotManager slots = SkillCaster.SkillSlots;
			if (slots == null)
			{
				return -1;
			}

			int slotCount = slots.SlotCount;
			for (int i = 0; i < slotCount; i++)
			{
				ActiveSkillData active = slots.GetSkill(i) as ActiveSkillData;
				if (active != null && active.ActivateWithJumpButton)
				{
					return i;
				}
			}

			return -1;
		}
	}
}
