using System.Collections.Generic;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	/// <summary>
	/// Session-wide store for the player's owned/equipped skills.
	///
	/// Skill assets are ScriptableObjects, so their C# references stay valid
	/// across scene loads while this store is alive. The player character is
	/// re-spawned by LevelManager on every scene, so SkillSlotManager.Awake
	/// reads this store to re-apply the saved slot layout to the new player.
	/// </summary>
	public static class SkillSaveStore
	{
		private static readonly List<Skill> _owned = new List<Skill>();
		private static readonly List<Skill> _equipped = new List<Skill>();

		/// <summary>
		/// True once any save data has been recorded this session.
		/// </summary>
		public static bool HasData { get; private set; }

		public static IReadOnlyList<Skill> Owned => _owned;

		public static IReadOnlyList<Skill> Equipped => _equipped;

		public static void Reset()
		{
			_owned.Clear();
			_equipped.Clear();
			HasData = false;
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
		}
	}
}
