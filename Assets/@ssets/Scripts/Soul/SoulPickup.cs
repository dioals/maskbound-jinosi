using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Soul
{
	[AddComponentMenu("Maskbound/Soul/Soul Pickup")]
	public class SoulPickup : PickableItem
	{
		[Header("Soul")]
		public int SoulToAdd = 10;

		protected override void Pick(GameObject picker)
		{
			SoulWallet wallet = picker.GetComponentInParent<SoulWallet>();
			if (wallet != null)
			{
				wallet.AddSoul(SoulToAdd);
				return;
			}

			SoulWallet.Add(SoulToAdd);
		}
	}
}
