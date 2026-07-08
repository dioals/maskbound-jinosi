using MaskboundJinosi.Soul;
using TMPro;
using UnityEngine;

namespace MaskboundJinosi.UI
{
	[AddComponentMenu("Maskbound/UI/Soul Text Display")]
	public class SoulTextDisplay : MonoBehaviour
	{
		[Header("References")]
		public TMP_Text TargetText;

		[Header("Format")]
		public string Prefix = "Soul: ";
		public string Suffix = "";
		public bool UseThousandsSeparator = false;

		protected virtual void Reset()
		{
			TargetText = GetComponent<TMP_Text>();
		}

		protected virtual void OnEnable()
		{
			SoulWallet.SoulChanged += HandleSoulChanged;
			Refresh();
		}

		protected virtual void OnDisable()
		{
			SoulWallet.SoulChanged -= HandleSoulChanged;
		}

		public virtual void Refresh()
		{
			HandleSoulChanged(SoulWallet.CurrentSoul);
		}

		protected virtual void HandleSoulChanged(int amount)
		{
			if (TargetText == null)
			{
				return;
			}

			string value = UseThousandsSeparator ? amount.ToString("N0") : amount.ToString();
			TargetText.text = $"{Prefix}{value}{Suffix}";
		}
	}
}
