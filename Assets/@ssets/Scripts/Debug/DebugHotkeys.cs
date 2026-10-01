#if UNITY_EDITOR || DEVELOPMENT_BUILD
using MaskboundJinosi.UI;
using MoreMountains.CorgiEngine;
using UnityEngine;

namespace MaskboundJinosi.Debugging
{
	/// <summary>
	/// QA hotkeys, compiled only into the Editor and Development Builds.
	/// Spawned automatically before the first scene loads so it works in any scene,
	/// with or without the Bootstrap scene / DevTestHub.
	///   F11 - kill the player
	///   F12 - kill the active boss
	/// </summary>
	[AddComponentMenu("")]
	public class DebugHotkeys : MonoBehaviour
	{
		public KeyCode KillPlayerKey = KeyCode.F11;
		public KeyCode KillBossKey = KeyCode.F12;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void CreateInstance()
		{
			GameObject hotkeys = new GameObject("DebugHotkeys_AutoCreated");
			hotkeys.AddComponent<DebugHotkeys>();
			DontDestroyOnLoad(hotkeys);
		}

		protected virtual void Update()
		{
			if (UnityEngine.Input.GetKeyDown(KillPlayerKey))
			{
				ForceKill(ResolvePlayerHealth(), "Player");
			}

			if (UnityEngine.Input.GetKeyDown(KillBossKey))
			{
				ForceKill(ResolveBossHealth(), "Boss");
			}
		}

		protected virtual void ForceKill(Health health, string label)
		{
			if (health == null)
			{
				UnityEngine.Debug.LogWarning($"[DebugHotkeys] {label} not found", this);
				return;
			}

			if (health.CurrentHealth <= 0f)
			{
				UnityEngine.Debug.LogWarning($"[DebugHotkeys] {label} is already dead", this);
				return;
			}

			// Health.Kill() silently no-ops while ImmuneToDamage is set (e.g. boss phase transitions).
			health.ImmuneToDamage = false;
			health.Kill();
			UnityEngine.Debug.Log($"[DebugHotkeys] {label} killed: {health.name}", this);
		}

		protected virtual Health ResolvePlayerHealth()
		{
			LevelManager levelManager = LevelManager.Instance;
			if (levelManager != null && levelManager.Players != null && levelManager.Players.Count > 0 && levelManager.Players[0] != null)
			{
				return levelManager.Players[0].CharacterHealth;
			}

			Character[] characters = FindObjectsByType<Character>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (Character character in characters)
			{
				if (character.CharacterType == Character.CharacterTypes.Player)
				{
					return character.CharacterHealth;
				}
			}

			return null;
		}

		protected virtual Health ResolveBossHealth()
		{
			if (BossHealthTarget.Current != null && BossHealthTarget.Current.Health != null)
			{
				return BossHealthTarget.Current.Health;
			}

			BossHealthTarget[] bosses = FindObjectsByType<BossHealthTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (BossHealthTarget boss in bosses)
			{
				if (boss.Health != null && boss.Health.CurrentHealth > 0f)
				{
					return boss.Health;
				}
			}

			return null;
		}
	}
}
#endif
