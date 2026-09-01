using System.Collections.Generic;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	/// <summary>
	/// Session-wide store for the player's owned/equipped skills, persisted to
	/// PlayerPrefs so purchases survive across game sessions (not just scenes).
	///
	/// Skills are referenced by their unique SkillId. Because ScriptableObject
	/// references stay valid across scene loads within a session, the in-memory
	/// lists hold direct references; the PlayerPrefs snapshot is written on every
	/// change and read once at startup to re-seed those lists.
	/// </summary>
	public static class SkillSaveStore
	{
		private const string OwnedKey = "Maskbound.SkillSave.Owned";
		private const string EquippedKey = "Maskbound.SkillSave.Equipped";
		private const char Separator = ',';

		private static readonly List<Skill> _owned = new List<Skill>();
		private static readonly List<Skill> _equipped = new List<Skill>();
		private static readonly Dictionary<string, Skill> _skillById = new Dictionary<string, Skill>();
		private static bool _loaded;

		/// <summary>
		/// True once any save data has been recorded this session.
		/// </summary>
		public static bool HasData { get; private set; }

		public static IReadOnlyList<Skill> Owned => _owned;

		public static IReadOnlyList<Skill> Equipped => _equipped;

		/// <summary>
		/// Registers a skill so its SkillId can be resolved when loading a save.
		/// Every skill the game can reference (shop stock, starter skills) must be
		/// registered before Load() is called, otherwise it will be skipped.
		/// </summary>
		public static void Register(Skill skill)
		{
			if (skill == null || string.IsNullOrEmpty(skill.SkillId))
			{
				return;
			}

			_skillById[skill.SkillId] = skill;
		}

		/// <summary>
		/// Loads owned/equipped skills from PlayerPrefs into memory. Safe to call
		/// multiple times; only the first call (per session) reads the save.
		/// </summary>
		public static void Load()
		{
			if (_loaded)
			{
				return;
			}

			_loaded = true;
			_owned.Clear();
			_equipped.Clear();

			ReadInto(OwnedKey, _owned);
			ReadInto(EquippedKey, _equipped);

			HasData = _owned.Count > 0 || _equipped.Count > 0;
		}

		/// <summary>
		/// Clears all in-memory data AND the PlayerPrefs save. Called on New Game.
		/// </summary>
		public static void Reset()
		{
			_owned.Clear();
			_equipped.Clear();
			HasData = false;
			_loaded = false;

			PlayerPrefs.DeleteKey(OwnedKey);
			PlayerPrefs.DeleteKey(EquippedKey);
		}

		/// <summary>
		/// Wipes the PlayerPrefs save only, keeping in-memory data intact.
		/// </summary>
		public static void ClearPersistedData()
		{
			PlayerPrefs.DeleteKey(OwnedKey);
			PlayerPrefs.DeleteKey(EquippedKey);
		}

		public static bool IsOwned(Skill skill)
		{
			return skill != null && _owned.Contains(skill);
		}

		public static bool IsEquipped(Skill skill)
		{
			return skill != null && _equipped.Contains(skill);
		}

		public static void MarkOwned(Skill skill)
		{
			if (skill == null || IsOwned(skill))
			{
				return;
			}

			_owned.Add(skill);
			HasData = true;
			PersistOwned();
		}

		/// <summary>
		/// Records the full equipped-slot layout. Slot count must match the
		/// player's slot count (null = empty slot).
		/// </summary>
		public static void SaveSlots(IReadOnlyList<Skill> slots)
		{
			_equipped.Clear();

			if (slots != null)
			{
				foreach (Skill skill in slots)
				{
					_equipped.Add(skill);
				}
			}

			if (_equipped.Count > 0)
			{
				HasData = true;
			}

			PersistEquipped();
		}

		private static void ReadInto(string key, List<Skill> target)
		{
			string raw = PlayerPrefs.GetString(key, "");
			if (string.IsNullOrEmpty(raw))
			{
				return;
			}

			string[] ids = raw.Split(Separator);
			foreach (string id in ids)
			{
				if (string.IsNullOrEmpty(id))
				{
					continue;
				}

				if (_skillById.TryGetValue(id, out Skill skill) && !target.Contains(skill))
				{
					target.Add(skill);
				}
			}
		}

		private static void PersistOwned()
		{
			PlayerPrefs.SetString(OwnedKey, Join(_owned));
			PlayerPrefs.Save();
		}

		private static void PersistEquipped()
		{
			PlayerPrefs.SetString(EquippedKey, Join(_equipped));
			PlayerPrefs.Save();
		}

		private static string Join(List<Skill> skills)
		{
			if (skills.Count == 0)
			{
				return "";
			}

			var sb = new System.Text.StringBuilder();
			foreach (Skill skill in skills)
			{
				if (skill == null || string.IsNullOrEmpty(skill.SkillId))
				{
					sb.Append(Separator);
					continue;
				}

				sb.Append(skill.SkillId);
				sb.Append(Separator);
			}

			if (sb.Length > 0)
			{
				sb.Length--; // trailing separator
			}

			return sb.ToString();
		}
	}
}
