using UnityEngine;

namespace MaskboundJinosi.Stats
{
	[CreateAssetMenu(fileName = "CharacterStatData", menuName = "Maskbound/Stats/Character Stat Data")]
	public class CharacterStatData : ScriptableObject
	{
		[Header("Combat")]
		public float BaseAttackDamage = 10f;
	}
}
