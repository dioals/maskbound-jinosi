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
	/// Execution order matters here, hence the explicit attribute. We need to read the jump button after
	/// Character.Update has run CharacterJump.HandleInput (so we only react to presses the normal jump
	/// refused), but before InputManager.LateUpdate runs ProcessButtonStates, which flips ButtonDown to
	/// ButtonPressed and would hide the press from us. A late-ordered Update sits exactly in that gap:
	/// every Update runs before every LateUpdate, and the order value puts us after the default-order
	/// InputManager and Character. LateUpdate would be a coin flip against InputManager's own LateUpdate.
	///
	/// The skill is gated per airborne stretch, not by its cooldown: one cast per time in the air, and it
	/// comes back the moment the character touches ground.
	/// </summary>
	[AddComponentMenu("Maskbound/Skills/Character Jump Skill Input")]
	[DefaultExecutionOrder(100)]
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

			// Silent when wired correctly. If any of these is missing the component is simply dead, and
			// that is very hard to tell apart from "the skill isn't equipped" while playing.
			if (_character == null || _jump == null || _controller == null || SkillCaster == null)
			{
				Debug.LogError($"CharacterJumpSkillInput is missing references and will do nothing - " +
				               $"Character: {_character != null}, CharacterJump: {_jump != null}, " +
				               $"CorgiController: {_controller != null}, SkillCaster: {SkillCaster != null}", this);
			}
		}

		protected virtual void Update()
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
				LogBlocked("jump ability disabled or not authorized");
				return;
			}

			// A regular, coyote or buffered jump already consumed this press
			if (_jump.JumpHappenedThisFrame)
			{
				LogBlocked("a normal jump already happened this frame");
				return;
			}

			// Only step in where CharacterJump refused: airborne with nothing left.
			// NumberOfJumpsLeft > 0 also covers the coyote window, which stays a normal jump.
			if (_controller.State.IsGrounded || _jump.NumberOfJumpsLeft > 0)
			{
				LogBlocked($"grounded ({_controller.State.IsGrounded}) or jumps still left ({_jump.NumberOfJumpsLeft})");
				return;
			}

			if (usedSinceGrounded)
			{
				LogBlocked("already used since last touching ground");
				return;
			}

			int slotIndex = FindJumpButtonSkillSlot();
			if (slotIndex < 0)
			{
				LogBlocked("no equipped skill has ActivateWithJumpButton ticked");
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
			else
			{
				// CanCast said no - casting, blocking, mid-attack, or inside the 0.4s global cooldown
				LogBlocked($"slot {slotIndex} refused the cast (CharacterSkillCaster.CanCast)");
			}
		}

		/// <summary>
		/// Explains, under LogDebug, why a jump press did not turn into a skill cast. Every early exit
		/// below the button check reports, so a silent double jump can be diagnosed from the console
		/// instead of by guessing which gate closed.
		/// </summary>
		protected virtual void LogBlocked(string reason)
		{
			if (LogDebug)
			{
				Debug.Log($"Jump button press not routed to a skill: {reason}", this);
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
