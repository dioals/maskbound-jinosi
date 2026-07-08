using UnityEngine;

namespace MaskboundJinosi.Soul
{
	[CreateAssetMenu(fileName = "SoulCurrencyData", menuName = "Maskbound/Soul/Soul Currency Data")]
	public class SoulCurrencyData : ScriptableObject
	{
		public string DisplayName = "Soul";
		public int StartingAmount = 0;
		public int MaximumAmount = 999999;
	}
}
