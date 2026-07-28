using UnityEngine;

namespace MaskboundJinosi.Skills
{
	[AddComponentMenu("Maskbound/Skills/Character Skill Keyboard Input")]
	public class CharacterSkillKeyboardInput : MonoBehaviour
	{
		public CharacterSkillCaster SkillCaster;
		public KeyCode PrimarySkillKey = KeyCode.Alpha1;
		public KeyCode SecondarySkillKey = KeyCode.Alpha2;
		public KeyCode ThirdSkillKey = KeyCode.Alpha3;
		public KeyCode FourthSkillKey = KeyCode.Alpha4;

		protected virtual void Awake()
		{
			if (SkillCaster == null)
			{
				SkillCaster = GetComponentInChildren<CharacterSkillCaster>(true);
			}
		}

		protected virtual void Update()
		{
			if (SkillCaster == null)
			{
				return;
			}

			if (UnityEngine.Input.GetKeyDown(PrimarySkillKey))
			{
				SkillCaster.ActivateSkillSlot(0);
			}
			if (UnityEngine.Input.GetKeyDown(SecondarySkillKey))
			{
				SkillCaster.ActivateSkillSlot(1);
			}
			if (UnityEngine.Input.GetKeyDown(ThirdSkillKey))
			{
				SkillCaster.ActivateSkillSlot(2);
			}
			if (UnityEngine.Input.GetKeyDown(FourthSkillKey))
			{
				SkillCaster.ActivateSkillSlot(3);
			}
		}
	}
}
