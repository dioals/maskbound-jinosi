using System;
using MaskboundJinosi.Skills;
using MaskboundJinosi.Soul;
using MoreMountains.CorgiEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskboundJinosi.Debugging
{
	[AddComponentMenu("Maskbound/Debug/Developer Test Hub")]
	public class DevTestHub : MonoBehaviour
	{
		[Header("Target (optional)")]
		[SerializeField] protected Character _player;

		[Header("Test Values")]
		[Min(0f)] public float DamageAmount = 10f;
		[Min(0f)] public float HealAmount = 10f;
		[Min(0)] public int SoulAmount = 10;
		[Min(0)] public int SkillSlotIndex;

		[Header("Placeholder Skills")]
		public Skill PlaceholderPassiveSkill;
		public Skill PlaceholderActiveSkill;

		[Header("Build Safety")]
		[Tooltip("Keep this disabled so the hub automatically hides outside Editor and Development Builds.")]
		public bool AllowInReleaseBuild;

		public event Action<string> ActionPerformed;

		public Character Player => ResolvePlayer();
		public Health PlayerHealth => Player != null ? Player.CharacterHealth : null;
		public SkillSlotManager SkillSlots => Player != null ? Player.GetComponentInChildren<SkillSlotManager>(true) : null;
		public string LastAction { get; protected set; } = "Ready";

		protected virtual void Awake()
		{
			if (!IsDebugBuildAllowed())
			{
				gameObject.SetActive(false);
			}
		}

		public virtual void RefreshPlayerReference()
		{
			_player = null;
			ResolvePlayer();
			Report(_player != null ? $"Player found: {_player.name}" : "Player not found", _player != null);
		}

		public virtual void DamagePlayer()
		{
			Health health = PlayerHealth;
			if (!Require(health, "Health")) return;

			health.Damage(DamageAmount, gameObject, 0.1f, 0f, Vector3.zero);
			Report($"Damage {DamageAmount:0.##} | HP {health.CurrentHealth:0.##}/{health.MaximumHealth:0.##}");
		}

		public virtual void HealPlayer()
		{
			Health health = PlayerHealth;
			if (!Require(health, "Health")) return;

			health.SetHealth(Mathf.Min(health.CurrentHealth + HealAmount, health.MaximumHealth), gameObject);
			Report($"Heal {HealAmount:0.##} | HP {health.CurrentHealth:0.##}/{health.MaximumHealth:0.##}");
		}

		public virtual void HealPlayerToMaximum()
		{
			Health health = PlayerHealth;
			if (!Require(health, "Health")) return;

			health.ResetHealthToMaxHealth();
			Report($"HP restored to {health.CurrentHealth:0.##}");
		}

		public virtual void KillPlayer()
		{
			Health health = PlayerHealth;
			if (!Require(health, "Health")) return;

			health.Kill();
			Report("Player killed through Corgi Health");
		}

		public virtual void RevivePlayerHere()
		{
			Health health = PlayerHealth;
			if (!Require(health, "Health")) return;

			health.Revive();
			Report("Player revived at current position");
		}

		public virtual void RespawnAtCheckpoint()
		{
			Character player = Player;
			if (!Require(player, "Player")) return;

			LevelManager levelManager = LevelManager.Instance;
			if (levelManager != null && levelManager.CurrentCheckPoint != null)
			{
				levelManager.CurrentCheckPoint.SpawnPlayer(player);
				CorgiEngineEvent.Trigger(CorgiEngineEventTypes.Respawn, player);
				Report($"Respawned at checkpoint: {levelManager.CurrentCheckPoint.name}");
				return;
			}

			Health health = player.CharacterHealth;
			if (health != null)
			{
				health.Revive();
				Report("No checkpoint found; revived at current position", false);
				return;
			}

			Report("Respawn failed: Health not found", false);
		}

		public virtual void ToggleInvincibility()
		{
			Health health = PlayerHealth;
			if (!Require(health, "Health")) return;

			health.Invulnerable = !health.Invulnerable;
			Report($"Invincibility: {(health.Invulnerable ? "ON" : "OFF")}");
		}

		public virtual void AddSoul()
		{
			SoulWallet.Add(SoulAmount);
			Report($"Soul +{SoulAmount} | Total {SoulWallet.CurrentSoul}");
		}

		public virtual void SpendSoul()
		{
			bool spent = SoulWallet.Spend(SoulAmount);
			Report(spent
				? $"Soul -{SoulAmount} | Total {SoulWallet.CurrentSoul}"
				: $"Not enough Soul (need {SoulAmount}, have {SoulWallet.CurrentSoul})", spent);
		}

		public virtual void ResetSoul()
		{
			SoulWallet.ResetSessionSoul();
			Report("Soul reset to 0");
		}

		public virtual void ActivateSelectedSkill()
		{
			SkillSlotManager manager = SkillSlots;
			if (!Require(manager, "SkillSlotManager")) return;

			bool activated = manager.ActivateSkillInSlot(SkillSlotIndex);
			Report(activated
				? $"Activated skill in slot {SkillSlotIndex}"
				: $"Skill slot {SkillSlotIndex} could not activate", activated);
		}

		public virtual void EquipPlaceholderPassive()
		{
			EquipPlaceholder(PlaceholderPassiveSkill, "passive");
		}

		public virtual void EquipPlaceholderActive()
		{
			EquipPlaceholder(PlaceholderActiveSkill, "active");
		}

		public virtual void UnequipSelectedSkill()
		{
			SkillSlotManager manager = SkillSlots;
			if (!Require(manager, "SkillSlotManager")) return;

			bool unequipped = manager.Unequip(SkillSlotIndex);
			Report(unequipped
				? $"Unequipped skill from slot {SkillSlotIndex}"
				: $"Nothing to unequip in slot {SkillSlotIndex}", unequipped);
		}

		public virtual void AddSkillSlot()
		{
			SkillSlotManager manager = SkillSlots;
			if (!Require(manager, "SkillSlotManager")) return;

			int index = manager.AddSlot();
			Report($"Added skill slot {index} | Total {manager.SlotCount}");
		}

		public virtual void RemoveLastSkillSlot()
		{
			SkillSlotManager manager = SkillSlots;
			if (!Require(manager, "SkillSlotManager")) return;

			bool removed = manager.SlotCount > 0 && manager.RemoveSlot(manager.SlotCount - 1);
			Report(removed ? $"Removed last skill slot | Total {manager.SlotCount}" : "No skill slot to remove", removed);
		}

		public virtual void RestartCurrentScene()
		{
			Report("Restarting current scene");
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		public virtual void RestartCurrentSceneAndResetSoul()
		{
			SoulWallet.ResetSessionSoul();
			RestartCurrentScene();
		}

		protected virtual void EquipPlaceholder(Skill skill, string typeName)
		{
			SkillSlotManager manager = SkillSlots;
			if (!Require(manager, "SkillSlotManager") || !Require(skill, $"Placeholder {typeName} Skill")) return;

			bool equipped = manager.Equip(SkillSlotIndex, skill);
			Report(equipped
				? $"Equipped {skill.DisplayName} in slot {SkillSlotIndex}"
				: $"Could not equip {typeName} skill in slot {SkillSlotIndex}", equipped);
		}

		protected virtual Character ResolvePlayer()
		{
			if (_player != null) return _player;

			LevelManager levelManager = LevelManager.Instance;
			if (levelManager != null && levelManager.Players != null && levelManager.Players.Count > 0)
			{
				_player = levelManager.Players[0];
				return _player;
			}

			Character[] characters = FindObjectsByType<Character>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (Character character in characters)
			{
				if (character.CharacterType == Character.CharacterTypes.Player)
				{
					_player = character;
					break;
				}
			}

			return _player;
		}

		protected virtual bool Require(UnityEngine.Object reference, string label)
		{
			if (reference != null) return true;
			Report($"{label} not found", false);
			return false;
		}

		protected virtual void Report(string message, bool success = true)
		{
			LastAction = message;
			ActionPerformed?.Invoke(message);
			if (success) UnityEngine.Debug.Log($"[DevTestHub] {message}", this);
			else UnityEngine.Debug.LogWarning($"[DevTestHub] {message}", this);
		}

		protected virtual bool IsDebugBuildAllowed()
		{
			return AllowInReleaseBuild || UnityEngine.Debug.isDebugBuild || Application.isEditor;
		}
	}
}
