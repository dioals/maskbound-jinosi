using UnityEngine;

namespace MaskboundJinosi.Skills.Effects
{
	[AddComponentMenu("Maskbound/Skills/Effects/Ground Impact Skill Object Animation Relay")]
	public class GroundImpactSkillObjectAnimationRelay : MonoBehaviour
	{
		[SerializeField] private GroundImpactSkillObject2D target;
		[SerializeField] private bool logMissingTarget;

		private void Awake()
		{
			if (target == null)
			{
				target = GetComponentInParent<GroundImpactSkillObject2D>();
			}
		}

		public void ReleaseToDynamic()
		{
			if (target == null)
			{
				if (logMissingTarget)
				{
					Debug.LogWarning($"{name} has no GroundImpactSkillObject2D target.", this);
				}

				return;
			}

			target.ReleaseToDynamic();
		}
	}
}
