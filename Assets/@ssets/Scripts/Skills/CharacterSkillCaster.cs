using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Character Skill Caster")]
	public class CharacterSkillCaster : MonoBehaviour
	{
		[Header("References")]
		public SkillSlotManager SkillSlots;
		public Animator CharacterAnimator;
		public Transform SpawnOrigin;

		[Header("Input Slots")]
		public int PrimarySkillSlotIndex;
		public int SecondarySkillSlotIndex = 1;
		public int SelectedSkillSlotIndex;

		[Header("Runtime")]
		public bool LogDebug;

		protected readonly Dictionary<ActiveSkillData, float> _lastCastTimes = new Dictionary<ActiveSkillData, float>();
		protected Character _character;
		protected ActiveSkillData _currentSkill;
		protected SkillContext _currentSkillContext;
		protected bool _currentSkillFacingRight;

		protected virtual void Awake()
		{
			_character = GetComponentInParent<Character>();

			if (SkillSlots == null)
			{
				SkillSlots = GetComponentInChildren<SkillSlotManager>(true);
			}

			if (CharacterAnimator == null && _character != null)
			{
				CharacterAnimator = _character.CharacterAnimator;
			}

			if (SpawnOrigin == null)
			{
				SpawnOrigin = transform;
			}
		}

		public virtual bool ActivatePrimarySkill()
		{
			return ActivateSkillSlot(PrimarySkillSlotIndex);
		}

		public virtual bool ActivateSecondarySkill()
		{
			return ActivateSkillSlot(SecondarySkillSlotIndex);
		}

		public virtual bool ActivateSelectedSkill()
		{
			return ActivateSkillSlot(SelectedSkillSlotIndex);
		}

		public virtual void SelectSkillSlot(int slotIndex)
		{
			if (SkillSlots == null || SkillSlots.SlotCount <= 0)
			{
				SelectedSkillSlotIndex = 0;
				return;
			}

			SelectedSkillSlotIndex = Mathf.Clamp(slotIndex, 0, SkillSlots.SlotCount - 1);
		}

		public virtual void SelectNextSkillSlot()
		{
			SelectSkillSlot(SelectedSkillSlotIndex + 1);
		}

		public virtual void SelectPreviousSkillSlot()
		{
			SelectSkillSlot(SelectedSkillSlotIndex - 1);
		}

		public virtual bool ActivateSkillSlot(int slotIndex)
		{
			return SkillSlots != null && SkillSlots.ActivateSkillInSlot(slotIndex);
		}

		public virtual bool CanCast(ActiveSkillData skill)
		{
			if (skill == null)
			{
				return false;
			}

			return GetCooldownRemaining(skill) <= 0f;
		}

		public virtual float GetCooldownRemaining(ActiveSkillData skill)
		{
			if (skill == null)
			{
				return 0f;
			}

			if (!_lastCastTimes.TryGetValue(skill, out float lastCastTime))
			{
				return 0f;
			}

			return Mathf.Max(0f, skill.Cooldown - (Time.time - lastCastTime));
		}

		public virtual bool Cast(ActiveSkillData skill, SkillContext context)
		{
			if (!CanCast(skill))
			{
				return false;
			}

			bool facingRight = ResolveFacingRight();
			_lastCastTimes[skill] = Time.time;
			UpdateAnimator(skill);
			_currentSkill = skill;
			_currentSkillContext = context;
			_currentSkillFacingRight = facingRight;
			SpawnSkillPrefab(skill, context, facingRight);

			if (LogDebug)
			{
				Debug.Log($"Cast skill: {skill.DisplayName}", this);
			}

			return true;
		}

		public virtual void SpawnCurrentSkill()
		{
			SpawnCurrentSkillProjectile();
		}

		public virtual void SpawnCurrentSkillProjectile()
		{
			if (_currentSkill == null)
			{
				if (LogDebug)
				{
					Debug.LogWarning("SpawnCurrentSkillProjectile was called, but no active skill cast is available.", this);
				}

				return;
			}

			SpawnPrefab(
				_currentSkill,
				_currentSkillContext,
				_currentSkillFacingRight,
				_currentSkill.ProjectilePrefab,
				_currentSkill.ProjectileSpawnOffset,
				_currentSkill.ProjectileParentToOwner,
				_currentSkill.ProjectileMatchOwnerFacing);
		}

		protected virtual void UpdateAnimator(ActiveSkillData skill)
		{
			if (CharacterAnimator == null || skill == null)
			{
				return;
			}

			if (!string.IsNullOrEmpty(skill.SkillIndexParameter))
			{
				CharacterAnimator.SetInteger(skill.SkillIndexParameter, skill.SkillIndex);
			}

			if (!string.IsNullOrEmpty(skill.IsCastingParameter))
			{
				CharacterAnimator.SetBool(skill.IsCastingParameter, true);
			}

			if (!string.IsNullOrEmpty(skill.CastTriggerParameter))
			{
				CharacterAnimator.SetTrigger(skill.CastTriggerParameter);
			}
		}

		public virtual void StopCastingAnimation()
		{
			if (CharacterAnimator == null)
			{
				return;
			}

			CharacterAnimator.SetBool("IsCastingSkill", false);
		}

		protected virtual GameObject SpawnSkillPrefab(ActiveSkillData skill, SkillContext context, bool facingRight)
		{
			return SpawnPrefab(skill, context, facingRight, skill != null ? skill.SkillPrefab : null, skill != null ? skill.SpawnOffset : Vector2.zero, skill != null && skill.ParentToOwner, skill == null || skill.MatchOwnerFacing);
		}

		protected virtual GameObject SpawnPrefab(ActiveSkillData skill, SkillContext context, bool facingRight, GameObject prefab, Vector2 spawnOffset, bool parentToOwner, bool matchOwnerFacing)
		{
			if (skill == null || prefab == null)
			{
				return null;
			}
			
			Transform origin = SpawnOrigin != null ? SpawnOrigin : transform;
			Vector2 offset = spawnOffset;
			if (matchOwnerFacing)
			{
				offset.x = Mathf.Abs(offset.x) * (facingRight ? 1f : -1f);
			}

			Transform parent = parentToOwner ? origin : null;
			GameObject instance = Instantiate(prefab, origin.position + (Vector3)offset, Quaternion.identity, parent);

			if (matchOwnerFacing && !facingRight)
			{
				Vector3 scale = instance.transform.localScale;
				scale.x = -Mathf.Abs(scale.x);
				instance.transform.localScale = scale;
			}

			SkillRuntimeContext runtimeContext = new SkillRuntimeContext(skill, context, facingRight);
			ISkillRuntimeReceiver[] receivers = instance.GetComponentsInChildren<ISkillRuntimeReceiver>(true);
			for (int i = 0; i < receivers.Length; i++)
			{
				receivers[i].Initialize(runtimeContext);
			}

			return instance;
		}

		protected virtual bool ResolveFacingRight()
		{
			if (_character != null)
			{
				return _character.IsFacingRight;
			}

			return transform.lossyScale.x >= 0f;
		}
	}
}
