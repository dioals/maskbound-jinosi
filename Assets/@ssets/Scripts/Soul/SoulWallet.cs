using System;
using UnityEngine;

namespace MaskboundJinosi.Soul
{
	[AddComponentMenu("Maskbound/Soul/Soul Wallet")]
	public class SoulWallet : MonoBehaviour
	{
		[Header("Data")]
		public SoulCurrencyData CurrencyData;
		public bool InitializeFromDataIfEmpty = true;

		protected static bool _sessionInitialized;
		protected static int _sessionSoul;

		public static event Action<int> SoulChanged;

		public static int CurrentSoul => _sessionSoul;
		public int Amount => _sessionSoul;
		public int MaximumAmount => CurrencyData != null ? CurrencyData.MaximumAmount : int.MaxValue;

		protected virtual void Awake()
		{
			if (!_sessionInitialized)
			{
				_sessionSoul = (InitializeFromDataIfEmpty && CurrencyData != null) ? Mathf.Max(0, CurrencyData.StartingAmount) : 0;
				_sessionInitialized = true;
				SoulChanged?.Invoke(_sessionSoul);
			}
		}

		public virtual int AddSoul(int amount)
		{
			return Add(amount, MaximumAmount);
		}

		public virtual bool SpendSoul(int amount)
		{
			return Spend(amount);
		}

		public virtual bool CanSpendSoul(int amount)
		{
			return CanSpend(amount);
		}

		public static int Add(int amount, int maximumAmount = int.MaxValue)
		{
			if (amount <= 0)
			{
				return _sessionSoul;
			}

			_sessionSoul = Mathf.Clamp(_sessionSoul + amount, 0, maximumAmount);
			_sessionInitialized = true;
			SoulChanged?.Invoke(_sessionSoul);
			return _sessionSoul;
		}

		public static bool CanSpend(int amount)
		{
			return amount >= 0 && _sessionSoul >= amount;
		}

		public static bool Spend(int amount)
		{
			if (!CanSpend(amount))
			{
				return false;
			}

			_sessionSoul -= amount;
			_sessionInitialized = true;
			SoulChanged?.Invoke(_sessionSoul);
			return true;
		}

		public static void SetSoulForSession(int amount)
		{
			_sessionSoul = Mathf.Max(0, amount);
			_sessionInitialized = true;
			SoulChanged?.Invoke(_sessionSoul);
		}

		public static void ResetSessionSoul(int amount = 0)
		{
			SetSoulForSession(amount);
		}
	}
}
