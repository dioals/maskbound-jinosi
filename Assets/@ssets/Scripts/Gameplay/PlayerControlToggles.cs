using MaskboundJinosi.Skills;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Gameplay
{
	/// <summary>
	/// Runtime toggles for the player's gameplay controls. Each ability gets its
	/// own DisableX/EnableX pair so callers can switch off exactly what they need
	/// (e.g. keep only walk + interact) and restore everything afterwards.
	///
	/// Disabling is stateful on purpose: an in-progress dash/run/block/cast is
	/// stopped and cleaned up before the component is switched off, so nothing
	/// stays stuck (movement speed multiplier, MovementForbidden, hitbox, animator
	/// params). The previous component states are remembered and restored by the
	/// matching EnableX call.
	/// </summary>
	[AddComponentMenu("Maskbound/Gameplay/Player Control Toggles")]
	public class PlayerControlToggles : MonoBehaviour
	{
		[Header("References (auto-found on Awake if empty)")]
		[Tooltip("The player Character. Defaults to the one on this object.")]
		[SerializeField] private Character character;

		[Header("Tutorial Start Lock")]
		[Tooltip("On Start, disable every control except walk + interact until the tutorial unlocks them. Each ability is locked individually based on its own unlock key, so abilities already unlocked in an earlier tutorial stage/scene stay unlocked. Intended for the first scene(s) of a New Game (New Game wipes PlayerPrefs, so all keys are gone and the tutorial starts fully locked again).")]
		[SerializeField] private bool lockAllExceptWalkAndInteractOnStart;
		[Tooltip("PlayerPrefs key prefix used to persist each ability's unlocked state separately. 'Maskbound.TutorialUnlock' produces Maskbound.TutorialUnlock.Run, .Jump, .Dash, .Attack, .SpecialAttack, .Block, .Skills and .Pause. Every EnableX saves its own key; the Start lock only re-locks abilities whose key is not yet 1. Empty prefix = per-ability persistence off (the Start lock disables everything except walk + interact and nothing is saved).")]
		[SerializeField] private string unlockKeyPrefix = "Maskbound.TutorialUnlock";

		private const string RunId = "Run";
		private const string JumpId = "Jump";
		private const string DashId = "Dash";
		private const string AttackId = "Attack";
		private const string SpecialAttackId = "SpecialAttack";
		private const string BlockId = "Block";
		private const string SkillsId = "Skills";
		private const string PauseId = "Pause";

		[Header("Diagnostics")]
		[Tooltip("Log every enable/disable call.")]
		[SerializeField] private bool logToggles;

		private CharacterRun _run;
		private CharacterJump _jump;
		private CharacterDash _dash;
		private CharacterHandleWeapon _handleWeapon;
		private MaskboundJinosi.Combat.MaskboundSpecialAttackAbility _specialAttack;
		private MaskboundJinosi.Combat.CharacterBlock _block;
		private CharacterSkillCaster _skillCaster;
		private CharacterSkillSelectionInput _skillSelectionInput;
		private CharacterPause _pause;

		private bool _runWasEnabled;
		private bool _jumpWasEnabled;
		private bool _dashWasEnabled;
		private bool _weaponWasEnabled;
		private bool _specialWasEnabled;
		private bool _blockWasEnabled;
		private bool _skillCasterWasEnabled;
		private bool _skillInputWasEnabled;
		private bool _pauseWasEnabled;
		private bool _startLockApplied;

		protected virtual void Awake()
		{
			// Attached directly on the player prefab? Then the Character is already
			// here. Attached to a scene object instead? The player is spawned later
			// by LevelManager.Start(), so we wait in Update until it exists.
			if (character == null)
			{
				character = GetComponent<Character>();
			}

			if (character != null)
			{
				ResolvePlayer(character);
			}
		}

		protected virtual void Update()
		{
			if (!lockAllExceptWalkAndInteractOnStart)
			{
				return;
			}

			if (character == null)
			{
				// Scene-placed component: the player is spawned by LevelManager after
				// this object's Awake/Start, so keep looking until it appears.
				character = GetSpawnedPlayer();
				if (character == null)
				{
					return;
				}

				Log("Spawned player found: '" + character.name + "', resolving abilities.");
				ResolvePlayer(character);
			}

			// The spawned player's abilities initialize in their own Start, which
			// may still be pending this frame. Applying the lock before that would
			// snapshot half-initialized states, so wait until they are ready.
			if (!AreAbilitiesReady())
			{
				return;
			}

			if (!_startLockApplied)
			{
				RememberStates();
				ApplyStartLock();
				_startLockApplied = true;
				Debug.Log("[PlayerControlToggles] Start lock applied. Unlock keys -> " +
				          "Run=" + GetKeyValue(RunId) +
				          ", Jump=" + GetKeyValue(JumpId) +
				          ", Dash=" + GetKeyValue(DashId) +
				          ", Attack=" + GetKeyValue(AttackId) +
				          ", SpecialAttack=" + GetKeyValue(SpecialAttackId) +
				          ", Block=" + GetKeyValue(BlockId) +
				          ", Skills=" + GetKeyValue(SkillsId) +
				          ", Pause=" + GetKeyValue(PauseId) +
				          " (1 = already unlocked, skipped).", this);
				return;
			}

			// Keep the lock in place: systems that snapshot and restore the player's
			// ability states (e.g. PlayerRevive's spawn animation, which disables all
			// abilities and later restores them to their pre-revive state) would
			// otherwise re-enable abilities that are still locked. Re-apply the lock
			// whenever such a restore slips a locked ability back on.
			MaintainLock();
		}

		/// <summary>
		/// Re-disables any ability that is still locked (its unlock key is not set)
		/// but has been switched back on by another system since the last check.
		/// Abilities that were unlocked are never touched again.
		/// </summary>
		protected virtual void MaintainLock()
		{
			if (!IsUnlocked(RunId) && _run != null && _run.enabled) { DisableRun(); }
			if (!IsUnlocked(JumpId) && _jump != null && _jump.enabled) { DisableJump(); }
			if (!IsUnlocked(DashId) && _dash != null && _dash.enabled) { DisableDash(); }
			if (!IsUnlocked(AttackId) && _handleWeapon != null && _handleWeapon.enabled) { DisableAttack(); }
			if (!IsUnlocked(SpecialAttackId) && _specialAttack != null && _specialAttack.enabled) { DisableSpecialAttack(); }
			if (!IsUnlocked(BlockId) && _block != null && _block.enabled) { DisableBlock(); }
			if (!IsUnlocked(SkillsId) && (_skillCaster != null && _skillCaster.enabled || _skillSelectionInput != null && _skillSelectionInput.enabled)) { DisableSkills(); }
			if (!IsUnlocked(PauseId) && _pause != null && _pause.enabled) { DisablePause(); }
		}

		protected virtual bool AreAbilitiesReady()
		{
			CharacterAbility[] abilities = character.GetComponents<CharacterAbility>();
			for (int i = 0; i < abilities.Length; i++)
			{
				if (abilities[i].enabled && !abilities[i].AbilityInitialized)
				{
					Log("Waiting for ability '" + abilities[i].GetType().Name + "' to initialize...");
					return false;
				}
			}

			return true;
		}

		protected virtual void ResolvePlayer(Character target)
		{
			_run = target.FindAbility<CharacterRun>();
			_jump = target.FindAbility<CharacterJump>();
			_dash = target.FindAbility<CharacterDash>();
			_handleWeapon = target.FindAbility<CharacterHandleWeapon>();
			_specialAttack = target.FindAbility<MaskboundJinosi.Combat.MaskboundSpecialAttackAbility>();
			_block = target.FindAbility<MaskboundJinosi.Combat.CharacterBlock>();
			_pause = target.FindAbility<CharacterPause>();

			_skillCaster = target.GetComponentInChildren<CharacterSkillCaster>(true);
			_skillSelectionInput = target.GetComponentInChildren<CharacterSkillSelectionInput>(true);
		}

		protected virtual Character GetSpawnedPlayer()
		{
			if (LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0)
			{
				return LevelManager.Instance.Players[0];
			}

			return null;
		}

		/// <summary>
		/// Disables every ability whose unlock key is not yet set to 1, keeping
		/// walk + interact (not gated by any key) always available.
		/// </summary>
		protected virtual void ApplyStartLock()
		{
			if (IsUnlocked(RunId)) { Log("Run already unlocked, skipping disable."); } else { DisableRun(); }
			if (IsUnlocked(JumpId)) { Log("Jump already unlocked, skipping disable."); } else { DisableJump(); }
			if (IsUnlocked(DashId)) { Log("Dash already unlocked, skipping disable."); } else { DisableDash(); }
			if (IsUnlocked(AttackId)) { Log("Attack already unlocked, skipping disable."); } else { DisableAttack(); }
			if (IsUnlocked(SpecialAttackId)) { Log("SpecialAttack already unlocked, skipping disable."); } else { DisableSpecialAttack(); }
			if (IsUnlocked(BlockId)) { Log("Block already unlocked, skipping disable."); } else { DisableBlock(); }
			if (IsUnlocked(SkillsId)) { Log("Skills already unlocked, skipping disable."); } else { DisableSkills(); }
			if (IsUnlocked(PauseId)) { Log("Pause already unlocked, skipping disable."); } else { DisablePause(); }
		}

		/// <summary>
		/// Disables every control except walking (CharacterHorizontalMovement) and
		/// interacting (the InputManager Interact action, read directly by
		/// fountain/door/soul targets - not by an ability on the player).
		/// </summary>
		public virtual void DisableAllExceptWalkAndInteract()
		{
			// DisableRun();
			DisableJump();
			DisableDash();
			DisableAttack();
			DisableSpecialAttack();
			DisableBlock();
			DisableSkills();
			DisablePause();
		}

		/// <summary>
		/// Restores every ability that DisableAllExceptWalkAndInteract switched off
		/// to its previous state.
		/// </summary>
		public virtual void EnableAllExceptWalkAndInteract()
		{
			EnableRun();
			EnableJump();
			EnableDash();
			EnableAttack();
			EnableSpecialAttack();
			EnableBlock();
			EnableSkills();
			EnablePause();
		}

		/// <summary>
		/// Convenience for a single tutorial that unlocks everything at once:
		/// enables all abilities and persists every unlock key. Equivalent to
		/// calling each EnableX (which each save their own key).
		/// </summary>
		public virtual void UnlockAllAndSave()
		{
			EnableAllExceptWalkAndInteract();
			Log("All abilities unlocked and saved.");
		}

		#region Per-ability toggles

		public virtual void DisableRun()
		{
			if (_run == null || !_run.enabled)
			{
				return;
			}

			StopRunIfRunning();
			_run.enabled = false;
			Log("Run disabled");
		}

		public virtual void EnableRun()
		{
			if (_run == null)
			{
				return;
			}

			_run.enabled = _runWasEnabled;
			UnlockAndSave(RunId);
		}

		public virtual void DisableJump()
		{
			if (_jump == null || !_jump.enabled)
			{
				return;
			}

			_jump.enabled = false;
			Log("Jump disabled");
		}

		public virtual void EnableJump()
		{
			if (_jump == null)
			{
				return;
			}

			_jump.enabled = _jumpWasEnabled;
			UnlockAndSave(JumpId);
		}

		public virtual void DisableDash()
		{
			if (_dash == null || !_dash.enabled)
			{
				return;
			}

			if (IsDashing())
			{
				_dash.StopDash();
				SetMovementStateIdleIfGrounded();
			}

			_dash.enabled = false;
			Log("Dash disabled");
		}

		public virtual void EnableDash()
		{
			if (_dash == null)
			{
				return;
			}

			_dash.enabled = _dashWasEnabled;
			UnlockAndSave(DashId);
		}

		public virtual void DisableAttack()
		{
			if (_handleWeapon == null || !_handleWeapon.enabled)
			{
				return;
			}

			if (_handleWeapon.CurrentWeapon != null)
			{
				_handleWeapon.ForceStop();
			}

			_handleWeapon.enabled = false;
			Log("Attack (handle weapon) disabled");
		}

		public virtual void EnableAttack()
		{
			if (_handleWeapon == null)
			{
				return;
			}

			_handleWeapon.enabled = _weaponWasEnabled;
			UnlockAndSave(AttackId);
		}

		public virtual void DisableSpecialAttack()
		{
			if (_specialAttack == null || !_specialAttack.enabled)
			{
				return;
			}

			if (_specialAttack.IsSpecialAttacking)
			{
				_specialAttack.StopSpecialAttack();
			}

			_specialAttack.enabled = false;
			Log("Special attack disabled");
		}

		public virtual void EnableSpecialAttack()
		{
			if (_specialAttack == null)
			{
				return;
			}

			_specialAttack.enabled = _specialWasEnabled;
			UnlockAndSave(SpecialAttackId);
		}

		public virtual void DisableBlock()
		{
			if (_block == null || !_block.enabled)
			{
				return;
			}

			if (_block.IsBlocking)
			{
				_block.ReleaseBlockMovement();
				_block.BlockStop();
			}

			_block.enabled = false;
			Log("Block disabled");
		}

		public virtual void EnableBlock()
		{
			if (_block == null)
			{
				return;
			}

			_block.enabled = _blockWasEnabled;
			UnlockAndSave(BlockId);
		}

		public virtual void DisableSkills()
		{
			bool casterWasOn = _skillCaster != null && _skillCaster.enabled;
			bool inputWasOn = _skillSelectionInput != null && _skillSelectionInput.enabled;

			if (!casterWasOn && !inputWasOn)
			{
				return;
			}

			if (casterWasOn)
			{
				_skillCaster.StopCastingAnimation();
			}

			if (_skillCaster != null)
			{
				_skillCaster.enabled = false;
			}

			if (_skillSelectionInput != null)
			{
				_skillSelectionInput.enabled = false;
			}

			Log("Skills disabled (caster was " + casterWasOn + ", input was " + inputWasOn + ")");
		}

		public virtual void EnableSkills()
		{
			if (_skillCaster != null)
			{
				_skillCaster.enabled = _skillCasterWasEnabled;
			}

			if (_skillSelectionInput != null)
			{
				_skillSelectionInput.enabled = _skillInputWasEnabled;
			}

			UnlockAndSave(SkillsId);
		}

		public virtual void DisablePause()
		{
			if (_pause == null || !_pause.enabled)
			{
				return;
			}

			_pause.enabled = false;
			Log("Pause disabled");
		}

		public virtual void EnablePause()
		{
			if (_pause == null)
			{
				return;
			}

			_pause.enabled = _pauseWasEnabled;
			UnlockAndSave(PauseId);
		}

		#endregion

		#region Helpers

		protected virtual void StopRunIfRunning()
		{
			if (_run != null && _run.enabled && character != null
			    && character.MovementState.CurrentState == CharacterStates.MovementStates.Running)
			{
				_run.RunStop();
			}
		}

		protected virtual bool IsDashing()
		{
			return character != null
			       && character.MovementState.CurrentState == CharacterStates.MovementStates.Dashing;
		}

		protected virtual void SetMovementStateIdleIfGrounded()
		{
			CorgiController controller = character != null ? character.GetComponent<CorgiController>() : null;
			if (controller != null && controller.State.IsGrounded)
			{
				character.MovementState.ChangeState(CharacterStates.MovementStates.Idle);
			}
		}

		protected virtual string GetKey(string abilityId)
		{
			return string.IsNullOrEmpty(unlockKeyPrefix)
				? null
				: unlockKeyPrefix + "." + abilityId;
		}

		protected virtual int GetKeyValue(string abilityId)
		{
			string key = GetKey(abilityId);
			return key == null ? -1 : PlayerPrefs.GetInt(key, 0);
		}

		public virtual bool IsUnlocked(string abilityId)
		{
			string key = GetKey(abilityId);
			return key != null && PlayerPrefs.GetInt(key, 0) == 1;
		}

		protected virtual void UnlockAndSave(string abilityId)
		{
			string key = GetKey(abilityId);
			if (key == null)
			{
				Log(abilityId + " enabled (persistence off - 'Unlock Key Prefix' is empty).");
				return;
			}

			PlayerPrefs.SetInt(key, 1);
			PlayerPrefs.Save();
			Log(abilityId + " enabled and saved under '" + key + "'.");
		}

		protected virtual void RememberStates()
		{
			// The "was enabled" flags drive EnableX's restore target. They must
			// reflect the ability's normal/prefab state, NOT the transient state at
			// resolve time: systems like PlayerRevive disable every ability the
			// moment the player spawns (and restore them later), so sampling
			// `.enabled` here would record "false" for abilities that should be on
			// and break every later unlock. All abilities this component manages are
			// enabled on the prefab by design, so presence implies enabled.
			_runWasEnabled = _run != null;
			_jumpWasEnabled = _jump != null;
			_dashWasEnabled = _dash != null;
			_weaponWasEnabled = _handleWeapon != null;
			_specialWasEnabled = _specialAttack != null;
			_blockWasEnabled = _block != null;
			_skillCasterWasEnabled = _skillCaster != null;
			_skillInputWasEnabled = _skillSelectionInput != null;
			_pauseWasEnabled = _pause != null;
		}

		protected virtual void Log(string message)
		{
			if (logToggles)
			{
				Debug.Log("[PlayerControlToggles] " + message, this);
			}
		}

		#endregion

		#region Testing (context menu)

		/// <summary>
		/// Context-menu helpers for quickly testing the toggles in Play mode.
		/// The "Enable" variants below are runtime-only and do NOT write the
		/// PlayerPrefs unlock keys, so testing never pollutes the save.
		/// </summary>
		protected virtual bool EnsureResolvedForTesting()
		{
			if (character == null)
			{
				character = GetComponent<Character>() ?? GetSpawnedPlayer();
			}

			if (character == null)
			{
				Debug.LogWarning("[PlayerControlToggles] Testing: no player found. Run the game first.", this);
				return false;
			}

			if (_run == null)
			{
				ResolvePlayer(character);
				RememberStates();
			}

			return true;
		}

		[ContextMenu("Testing/Disable All Except Walk + Interact (no save)")]
		protected virtual void TestDisableAll()
		{
			if (EnsureResolvedForTesting())
			{
				DisableAllExceptWalkAndInteract();
			}
		}

		[ContextMenu("Testing/Enable All (no save)")]
		protected virtual void TestEnableAll()
		{
			if (!EnsureResolvedForTesting())
			{
				return;
			}

			if (_run != null) { _run.enabled = _runWasEnabled; }
			if (_jump != null) { _jump.enabled = _jumpWasEnabled; }
			if (_dash != null) { _dash.enabled = _dashWasEnabled; }
			if (_handleWeapon != null) { _handleWeapon.enabled = _weaponWasEnabled; }
			if (_specialAttack != null) { _specialAttack.enabled = _specialWasEnabled; }
			if (_block != null) { _block.enabled = _blockWasEnabled; }
			if (_skillCaster != null) { _skillCaster.enabled = _skillCasterWasEnabled; }
			if (_skillSelectionInput != null) { _skillSelectionInput.enabled = _skillInputWasEnabled; }
			if (_pause != null) { _pause.enabled = _pauseWasEnabled; }

			Debug.Log("[PlayerControlToggles] Testing: all abilities enabled (runtime only, not saved).", this);
		}

		[ContextMenu("Testing/Unlock All + Save (persist)")]
		protected virtual void TestUnlockAllAndSave()
		{
			if (EnsureResolvedForTesting())
			{
				UnlockAllAndSave();
			}
		}

		[ContextMenu("Testing/Run - Disable (no save)")]
		protected virtual void TestDisableRun() { if (EnsureResolvedForTesting()) { DisableRun(); } }

		[ContextMenu("Testing/Run - Enable (no save)")]
		protected virtual void TestEnableRun() { if (EnsureResolvedForTesting() && _run != null) { _run.enabled = _runWasEnabled; } }

		[ContextMenu("Testing/Jump - Disable (no save)")]
		protected virtual void TestDisableJump()
		{
			if (!EnsureResolvedForTesting())
			{
				return;
			}

			Debug.Log("[PlayerControlToggles] Testing disable jump: _jump=" + _jump +
			          ", _jump.enabled=" + (_jump != null ? _jump.enabled.ToString() : "n/a") +
			          ", _jumpWasEnabled=" + _jumpWasEnabled +
			          ", player=" + (character != null ? character.name : "null"), this);
			DisableJump();
			Debug.Log("[PlayerControlToggles] After DisableJump: _jump.enabled=" + (_jump != null ? _jump.enabled.ToString() : "n/a"), this);
		}

		[ContextMenu("Testing/Jump - Enable (no save)")]
		protected virtual void TestEnableJump()
		{
			if (EnsureResolvedForTesting() && _jump != null)
			{
				_jump.enabled = _jumpWasEnabled;
				Debug.Log("[PlayerControlToggles] Testing enable jump: _jump.enabled=" + _jump.enabled, this);
			}
		}

		[ContextMenu("Testing/Dash - Disable (no save)")]
		protected virtual void TestDisableDash() { if (EnsureResolvedForTesting()) { DisableDash(); } }

		[ContextMenu("Testing/Dash - Enable (no save)")]
		protected virtual void TestEnableDash() { if (EnsureResolvedForTesting() && _dash != null) { _dash.enabled = _dashWasEnabled; } }

		[ContextMenu("Testing/Attack - Disable (no save)")]
		protected virtual void TestDisableAttack() { if (EnsureResolvedForTesting()) { DisableAttack(); } }

		[ContextMenu("Testing/Attack - Enable (no save)")]
		protected virtual void TestEnableAttack() { if (EnsureResolvedForTesting() && _handleWeapon != null) { _handleWeapon.enabled = _weaponWasEnabled; } }

		[ContextMenu("Testing/Special Attack - Disable (no save)")]
		protected virtual void TestDisableSpecialAttack() { if (EnsureResolvedForTesting()) { DisableSpecialAttack(); } }

		[ContextMenu("Testing/Special Attack - Enable (no save)")]
		protected virtual void TestEnableSpecialAttack() { if (EnsureResolvedForTesting() && _specialAttack != null) { _specialAttack.enabled = _specialWasEnabled; } }

		[ContextMenu("Testing/Block - Disable (no save)")]
		protected virtual void TestDisableBlock() { if (EnsureResolvedForTesting()) { DisableBlock(); } }

		[ContextMenu("Testing/Block - Enable (no save)")]
		protected virtual void TestEnableBlock() { if (EnsureResolvedForTesting() && _block != null) { _block.enabled = _blockWasEnabled; } }

		[ContextMenu("Testing/Skills - Disable (no save)")]
		protected virtual void TestDisableSkills() { if (EnsureResolvedForTesting()) { DisableSkills(); } }

		[ContextMenu("Testing/Skills - Enable (no save)")]
		protected virtual void TestEnableSkills() { if (EnsureResolvedForTesting() && _skillCaster != null) { _skillCaster.enabled = _skillCasterWasEnabled; } if (EnsureResolvedForTesting() && _skillSelectionInput != null) { _skillSelectionInput.enabled = _skillInputWasEnabled; } }

		[ContextMenu("Testing/Pause - Disable (no save)")]
		protected virtual void TestDisablePause() { if (EnsureResolvedForTesting()) { DisablePause(); } }

		[ContextMenu("Testing/Pause - Enable (no save)")]
		protected virtual void TestEnablePause() { if (EnsureResolvedForTesting() && _pause != null) { _pause.enabled = _pauseWasEnabled; } }

		#endregion
	}
}
