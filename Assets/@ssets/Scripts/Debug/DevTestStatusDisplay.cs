using MaskboundJinosi.Soul;
using TMPro;
using UnityEngine;

namespace MaskboundJinosi.Debugging
{
	[AddComponentMenu("Maskbound/Debug/Developer Test Status Display")]
	public class DevTestStatusDisplay : MonoBehaviour
	{
		public DevTestHub Hub;
		public TMP_Text TargetText;
		[Min(0.05f)] public float RefreshInterval = 0.2f;

		protected float _nextRefreshAt;

		protected virtual void Reset()
		{
			TargetText = GetComponent<TMP_Text>();
		}

		protected virtual void OnEnable()
		{
			SoulWallet.SoulChanged += HandleSoulChanged;
			if (Hub != null) Hub.ActionPerformed += HandleActionPerformed;
			Refresh();
		}

		protected virtual void OnDisable()
		{
			SoulWallet.SoulChanged -= HandleSoulChanged;
			if (Hub != null) Hub.ActionPerformed -= HandleActionPerformed;
		}

		protected virtual void Update()
		{
			if (Time.unscaledTime >= _nextRefreshAt)
			{
				Refresh();
			}
		}

		public virtual void Refresh()
		{
			_nextRefreshAt = Time.unscaledTime + RefreshInterval;
			if (TargetText == null || Hub == null) return;

			string health = Hub.PlayerHealth != null
				? $"{Hub.PlayerHealth.CurrentHealth:0.##}/{Hub.PlayerHealth.MaximumHealth:0.##}"
				: "N/A";
			string bossHealth = Hub.BossHealth != null
				? $"{Hub.BossHealth.CurrentHealth:0.##}/{Hub.BossHealth.MaximumHealth:0.##}"
				: "N/A";
			string invincible = Hub.PlayerHealth != null && Hub.PlayerHealth.Invulnerable ? "ON" : "OFF";
			string slots = Hub.SkillSlots != null ? Hub.SkillSlots.SlotCount.ToString() : "N/A";
			string timeScale = $"{Time.timeScale:0.##}x";

			TargetText.text =
				$"HP: {health}   Boss HP: {bossHealth}   Invincible: {invincible}\n" +
				$"Soul: {SoulWallet.CurrentSoul}   Skill Slots: {slots}   Time: {timeScale}\n" +
				$"Last: {Hub.LastAction}";
		}

		protected virtual void HandleSoulChanged(int amount) => Refresh();
		protected virtual void HandleActionPerformed(string message) => Refresh();
	}
}
