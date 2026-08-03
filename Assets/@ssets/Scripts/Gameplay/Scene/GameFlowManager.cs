using System;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MaskboundJinosi.Gameplay.Scene
{
	[AddComponentMenu("Maskbound/Scene/Game Flow Manager")]
	public class GameFlowManager : MonoBehaviour
	{
		private const string LastSceneKey = "Maskbound.LastGameplayScene";

		[Header("Scenes")]
		public string BootstrapSceneName = "Bootstrap";
		public string FirstGameplayScene = "Aras_Mamungkut_Forest";
		public string LoadingSceneName = "LoadingScreen";

		[Header("References")]
		public BootstrapSceneLoader SceneLoader;
		public GameObject MainMenuRoot;
		public GameObject[] GameplayUIRoots;
		public Button ContinueButton;
		public TMP_Text SettingsValueText;
		public GameObject[] DevelopmentOnlyObjects;

		[Header("Settings")]
		[Range(0f, 1f)] public float MasterVolume = 1f;
		[Range(0.05f, 0.5f)] public float VolumeStep = 0.1f;

		public event Action<string> StatusChanged;
		public bool CanContinue => PlayerPrefs.HasKey(LastSceneKey)
			&& !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(LastSceneKey));

		protected virtual void Awake()
		{
			if (SceneLoader == null)
			{
				SceneLoader = GetComponent<BootstrapSceneLoader>();
			}

			SceneManager.sceneLoaded += HandleSceneLoaded;
			AudioListener.volume = MasterVolume;
			SetDevelopmentObjectsVisible(Application.isEditor || Debug.isDebugBuild);
			RefreshUI();
		}

		protected virtual void Start()
		{
			if (SceneManager.GetActiveScene().name == BootstrapSceneName)
			{
				ShowMainMenu();
			}
		}

		public virtual void StartNewGame()
		{
			LoadGameplayScene(FirstGameplayScene);
		}

		public virtual void ContinueGame()
		{
			if (!CanContinue)
			{
				Report("No continue data available");
				return;
			}

			LoadGameplayScene(PlayerPrefs.GetString(LastSceneKey));
		}

		public virtual void LoadDeveloperScene(string sceneName)
		{
			if (!(Application.isEditor || Debug.isDebugBuild))
			{
				return;
			}

			LoadGameplayScene(sceneName);
		}

		public virtual void ReturnToMainMenu()
		{
			Time.timeScale = 1f;
			if (SceneManager.GetActiveScene().name == BootstrapSceneName)
			{
				ShowMainMenu();
				return;
			}

			HideAllPersistentUIForLoading();
			MMSceneLoadingManager.LoadScene(BootstrapSceneName, LoadingSceneName);
		}

		public virtual void ShowMainMenu()
		{
			SetMainMenuVisible(true);
			RefreshUI();
		}

		public virtual void HideMainMenu()
		{
			SetMainMenuVisible(false);
		}

		public virtual void IncreaseMasterVolume()
		{
			SetMasterVolume(MasterVolume + VolumeStep);
		}

		public virtual void DecreaseMasterVolume()
		{
			SetMasterVolume(MasterVolume - VolumeStep);
		}

		public virtual void SetMasterVolume(float value)
		{
			MasterVolume = Mathf.Clamp01(value);
			AudioListener.volume = MasterVolume;
			RefreshUI();
			Report($"Master volume: {Mathf.RoundToInt(MasterVolume * 100f)}%");
		}

		public virtual void ToggleFullscreen()
		{
			Screen.fullScreen = !Screen.fullScreen;
			RefreshUI();
			Report($"Fullscreen: {(Screen.fullScreen ? "ON" : "OFF")}");
		}

		public virtual void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		public virtual void ClearContinueData()
		{
			PlayerPrefs.DeleteKey(LastSceneKey);
			PlayerPrefs.Save();
			RefreshUI();
			Report("Continue data cleared");
		}

		private void LoadGameplayScene(string sceneName)
		{
			if (string.IsNullOrWhiteSpace(sceneName))
			{
				Report("Gameplay scene is empty");
				return;
			}

			Time.timeScale = 1f;
			HideAllPersistentUIForLoading();

			if (SceneLoader != null)
			{
				SceneLoader.LoadLevel(sceneName);
			}
			else
			{
				MMSceneLoadingManager.LoadScene(sceneName, LoadingSceneName);
			}
		}

		private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
		{
			if (scene.name == LoadingSceneName)
			{
				HideAllPersistentUIForLoading();
				return;
			}

			if (scene.name == BootstrapSceneName)
			{
				ShowMainMenu();
				return;
			}

			PlayerPrefs.SetString(LastSceneKey, scene.name);
			PlayerPrefs.Save();
			SetMainMenuVisible(false);
			RefreshUI();
		}

		private void SetMainMenuVisible(bool visible)
		{
			if (MainMenuRoot != null)
			{
				MainMenuRoot.SetActive(visible);
			}

			foreach (GameObject root in GameplayUIRoots)
			{
				if (root != null)
				{
					root.SetActive(!visible);
				}
			}
		}

		private void HideAllPersistentUIForLoading()
		{
			if (MainMenuRoot != null)
			{
				MainMenuRoot.SetActive(false);
			}

			if (GameplayUIRoots == null)
			{
				return;
			}

			foreach (GameObject root in GameplayUIRoots)
			{
				if (root != null)
				{
					root.SetActive(false);
				}
			}
		}

		private void SetDevelopmentObjectsVisible(bool visible)
		{
			foreach (GameObject target in DevelopmentOnlyObjects)
			{
				if (target != null)
				{
					target.SetActive(visible);
				}
			}
		}

		private void RefreshUI()
		{
			if (ContinueButton != null)
			{
				ContinueButton.interactable = CanContinue;
			}

			if (SettingsValueText != null)
			{
				SettingsValueText.text = $"Volume  {Mathf.RoundToInt(MasterVolume * 100f)}%    Fullscreen  {(Screen.fullScreen ? "ON" : "OFF")}";
			}
		}

		private void Report(string message)
		{
			StatusChanged?.Invoke(message);
			Debug.Log($"[GameFlow] {message}", this);
		}

		protected virtual void OnDestroy()
		{
			SceneManager.sceneLoaded -= HandleSceneLoaded;
		}
	}
}
