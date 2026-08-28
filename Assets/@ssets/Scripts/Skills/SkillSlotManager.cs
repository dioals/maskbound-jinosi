using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Skill Slot Manager")]
	[DefaultExecutionOrder(-100)]
	public class SkillSlotManager : MonoBehaviour
	{
		[Header("Slots")]
		[SerializeField] protected List<SkillSlot> _slots = new List<SkillSlot>();
		[SerializeField] protected int _initialSlotCount = 3;

		public IReadOnlyList<SkillSlot> Slots => _slots;
		public int SlotCount => _slots.Count;

		public event Action<int, Skill> SkillEquipped;
		public event Action<int, Skill> SkillUnequipped;
		public event Action<int, Skill> SkillActivated;
		public event Action<int> SlotCountChanged;

		protected virtual void Awake()
		{
			EnsureSlotCount(Mathf.Max(0, _initialSlotCount));
			RestoreFromSaveStore();
			ApplyEquippedPassives();
		}

		/// <summary>
		/// Re-applies the session's saved slot layout (SkillSaveStore) onto this
		/// manager. Used when the player is re-spawned on a new scene, so the
		/// skills equipped in the shop survive the scene change.
		/// </summary>
		protected virtual void RestoreFromSaveStore()
		{
			if (!SkillSaveStore.HasData || SkillSaveStore.Equipped.Count == 0)
			{
				return;
			}

			for (int i = 0; i < _slots.Count && i < SkillSaveStore.Equipped.Count; i++)
			{
				Skill skill = SkillSaveStore.Equipped[i];
				if (skill == null)
				{
					continue;
				}

				if (_slots[i].EquippedSkill == skill)
				{
					continue;
				}

				// No OnEquipped call here: ApplyEquippedPassives() handles passive
				// effects right after this, and active skills don't override
				// OnEquipped. Firing the event keeps slot UI listeners in sync.
				_slots[i].EquippedSkill = skill;
				SkillEquipped?.Invoke(i, skill);
			}
		}

		public virtual void SetSlotCount(int slotCount)
		{
			slotCount = Mathf.Max(0, slotCount);

			while (_slots.Count > slotCount)
			{
				RemoveSlot(_slots.Count - 1);
			}

			while (_slots.Count < slotCount)
			{
				AddSlot();
			}
		}

		public virtual int AddSlot()
		{
			_slots.Add(new SkillSlot());
			SlotCountChanged?.Invoke(_slots.Count);
			return _slots.Count - 1;
		}

		public virtual bool RemoveSlot(int slotIndex)
		{
			if (!IsValidSlot(slotIndex))
			{
				return false;
			}

			Unequip(slotIndex);
			_slots.RemoveAt(slotIndex);
			SlotCountChanged?.Invoke(_slots.Count);
			return true;
		}

		public virtual bool Equip(int slotIndex, Skill skill)
		{
			if (!IsValidSlot(slotIndex) || skill == null)
			{
				return false;
			}

			SkillContext context = new SkillContext(this, slotIndex);
			if (!skill.CanEquip(context))
			{
				return false;
			}

			Unequip(slotIndex);
			_slots[slotIndex].EquippedSkill = skill;
			skill.OnEquipped(context);
			SkillEquipped?.Invoke(slotIndex, skill);
			return true;
		}

		public virtual bool Unequip(int slotIndex)
		{
			if (!IsValidSlot(slotIndex))
			{
				return false;
			}

			Skill skill = _slots[slotIndex].EquippedSkill;
			if (skill == null)
			{
				return false;
			}

			SkillContext context = new SkillContext(this, slotIndex);
			skill.OnUnequipped(context);
			_slots[slotIndex].EquippedSkill = null;
			SkillUnequipped?.Invoke(slotIndex, skill);
			return true;
		}

		public virtual bool ActivateSkillInSlot(int slotIndex)
		{
			if (!IsValidSlot(slotIndex))
			{
				return false;
			}

			Skill skill = _slots[slotIndex].EquippedSkill;
			if (skill == null || skill.SkillType != SkillType.Active)
			{
				return false;
			}

			SkillContext context = new SkillContext(this, slotIndex);
			if (!skill.CanActivate(context))
			{
				return false;
			}

			bool activated = skill.Activate(context);
			if (activated)
			{
				SkillActivated?.Invoke(slotIndex, skill);
			}

			return activated;
		}

		public virtual Skill GetSkill(int slotIndex)
		{
			return IsValidSlot(slotIndex) ? _slots[slotIndex].EquippedSkill : null;
		}

		public virtual bool IsValidSlot(int slotIndex)
		{
			return slotIndex >= 0 && slotIndex < _slots.Count;
		}

		protected virtual void EnsureSlotCount(int slotCount)
		{
			while (_slots.Count < slotCount)
			{
				_slots.Add(new SkillSlot());
			}
		}

		protected virtual void ApplyEquippedPassives()
		{
			for (int i = 0; i < _slots.Count; i++)
			{
				Skill skill = _slots[i].EquippedSkill;
				if (skill != null && skill.SkillType == SkillType.Passive)
				{
					skill.OnEquipped(new SkillContext(this, i));
				}
			}
		}

		protected virtual void OnDestroy()
		{
			for (int i = 0; i < _slots.Count; i++)
			{
				if (_slots[i].EquippedSkill != null)
				{
					_slots[i].EquippedSkill.OnUnequipped(new SkillContext(this, i));
				}
			}
		}
	}
}
