using MoreMountains.Tools;
using MoreMountains.CorgiEngine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskboundJinosi.Gameplay.Scene
{
    [AddComponentMenu("Maskbound/Scene/Bootstrap Scene Loader")]
    [DefaultExecutionOrder(-10000)]
    public class BootstrapSceneLoader : MonoBehaviour
    {
        [Serializable]
        public class SceneMenuOption
        {
            public string Label;
            public string SceneName;
        }

        private static BootstrapSceneLoader _instance;
        private static bool _firstLevelLoaded;

        [Header("Persistent Root")]
        [SerializeField] private bool keepRootAlive = true;
        [SerializeField] private bool destroyDuplicateBootstrap = true;

        [Header("First Level")]
        [SerializeField] private bool loadFirstLevelOnStart = true;
        [SerializeField] private string firstLevelName = "Prototype_Level_01";
        [SerializeField] private string bootstrapSceneName = "Bootstrap";

        [Header("Start Scene Menu")]
        [SerializeField] private bool showSceneMenuOnStart = true;
        [SerializeField] private string sceneMenuTitle = "Select Scene";
        [SerializeField] private SceneMenuOption[] sceneMenuOptions =
        {
            new SceneMenuOption { Label = "Aras Mamungkut Forest", SceneName = "Aras_Mamungkut_Forest" },
            new SceneMenuOption { Label = "Daha Kingdom", SceneName = "Daha_Kingdom" },
            new SceneMenuOption { Label = "Sabrang Kingdom", SceneName = "Sabrang_Kingdom" },
            new SceneMenuOption { Label = "Sendang Sanctum", SceneName = "Sendang_Sanctum" },
            new SceneMenuOption { Label = "Prototype Level 01", SceneName = "Prototype_Level_01" }
        };
        [SerializeField] private Vector2 sceneMenuSize = new Vector2(420f, 360f);

        [Header("Loading")]
        [SerializeField] private bool useCorgiLoadingScreen = true;
        [SerializeField] private string loadingSceneName = "LoadingScreen";
        [SerializeField] private GameObject[] hideDuringLoading;

        [Header("Camera Target")]
        [SerializeField] private bool rebindCameraTargetAfterLevelLoad = true;
        [SerializeField] private int cameraRebindRetries = 20;
        [SerializeField] private float cameraRebindRetryDelay = 0.1f;

        public string FirstLevelName => firstLevelName;

        private bool _sceneMenuVisible;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _boxStyle;

        private void Awake()
        {
            if (destroyDuplicateBootstrap && _instance != null && _instance != this)
            {
                // Deactivate synchronously so sibling components (e.g. GUIManager) never run their own
                // Awake on this doomed duplicate root - Destroy() alone is deferred to end of frame and
                // would let them race in and steal static singleton references before cleanup happens.
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (keepRootAlive)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void Start()
        {
            if (showSceneMenuOnStart && !_firstLevelLoaded)
            {
                _sceneMenuVisible = true;
                SetLoadingHiddenObjectsActive(false);
                return;
            }

            if (!loadFirstLevelOnStart || _firstLevelLoaded)
            {
                return;
            }

            LoadFirstLevel();
        }

        public void LoadFirstLevel()
        {
            if (IsSceneLoaded(firstLevelName))
            {
                _firstLevelLoaded = true;
                return;
            }

            _firstLevelLoaded = true;
            LoadLevel(firstLevelName);
        }

        public void LoadLevel(string levelName)
        {
            if (string.IsNullOrWhiteSpace(levelName))
            {
                Debug.LogWarning("BootstrapSceneLoader cannot load an empty level name.", this);
                return;
            }

            _sceneMenuVisible = false;

            if (IsSceneLoaded(levelName))
            {
                return;
            }

            if (useCorgiLoadingScreen)
            {
                SetLoadingHiddenObjectsActive(false);
                MMSceneLoadingManager.LoadScene(levelName, loadingSceneName);
                return;
            }

            SetLoadingHiddenObjectsActive(false);
            SceneManager.LoadScene(levelName);
        }

        public void ResetFirstLevelLoadedFlag()
        {
            _firstLevelLoaded = false;
        }

        public void ShowSceneMenu()
        {
            ResetFirstLevelLoadedFlag();
            _sceneMenuVisible = true;
            SetLoadingHiddenObjectsActive(false);
            RebindPersistentGUI();
        }

        private void OnGUI()
        {
            if (!_sceneMenuVisible)
            {
                return;
            }

            EnsureSceneMenuStyles();

            float width = Mathf.Max(260f, sceneMenuSize.x);
            float height = Mathf.Max(220f, sceneMenuSize.y);
            Rect menuRect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUILayout.BeginArea(menuRect, _boxStyle);
            GUILayout.Space(16f);
            GUILayout.Label(sceneMenuTitle, _titleStyle);
            GUILayout.Space(18f);

            if (sceneMenuOptions != null && sceneMenuOptions.Length > 0)
            {
                for (int i = 0; i < sceneMenuOptions.Length; i++)
                {
                    SceneMenuOption option = sceneMenuOptions[i];
                    if (option == null || string.IsNullOrWhiteSpace(option.SceneName))
                    {
                        continue;
                    }

                    string label = string.IsNullOrWhiteSpace(option.Label) ? option.SceneName : option.Label;
                    if (GUILayout.Button(label, _buttonStyle, GUILayout.Height(44f)))
                    {
                        LoadLevel(option.SceneName);
                    }

                    GUILayout.Space(8f);
                }
            }
            else if (GUILayout.Button(firstLevelName, _buttonStyle, GUILayout.Height(44f)))
            {
                LoadFirstLevel();
            }

            GUILayout.EndArea();
        }

        private void EnsureSceneMenuStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(24, 24, 18, 24)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18
            };
        }

        private bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == sceneName && scene.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (scene.name == bootstrapSceneName)
            {
                if (showSceneMenuOnStart)
                {
                    ShowSceneMenu();
                }

                return;
            }

            RebindPersistentGUI();

            if (!rebindCameraTargetAfterLevelLoad || scene.name == loadingSceneName)
            {
                return;
            }

            SetLoadingHiddenObjectsActive(true);
            StartCoroutine(RebindCameraTargetWhenPlayerIsReady());
        }

        private void RebindPersistentGUI()
        {
            RepairPersistentCanvasScale();

            if (!GUIManager.HasInstance)
            {
                return;
            }

            GUIManager guiManager = GUIManager.Instance;
            if (guiManager.PauseScreen == null)
            {
                GameObject pauseScreen = FindNamedObject("PauseSplash");
                if (pauseScreen != null)
                {
                    guiManager.PauseScreen = pauseScreen;
                    pauseScreen.SetActive(false);
                }
            }

            if (guiManager.HUD == null)
            {
                GameObject hud = FindNamedObject("HUD");
                if (hud != null)
                {
                    guiManager.HUD = hud;
                }
            }
        }

        private void RepairPersistentCanvasScale()
        {
            GameObject canvas = FindNamedObject("Canvas");
            if (canvas == null)
            {
                return;
            }

            if (canvas.transform.localScale.sqrMagnitude <= 0.0001f)
            {
                canvas.transform.localScale = Vector3.one;
            }
        }

        private GameObject FindNamedObject(string objectName)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameObject target in objects)
            {
                if (target != null && target.name == objectName)
                {
                    return target;
                }
            }

            return null;
        }

        private void SetLoadingHiddenObjectsActive(bool active)
        {
            if (hideDuringLoading == null)
            {
                return;
            }

            foreach (GameObject target in hideDuringLoading)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }

        private IEnumerator RebindCameraTargetWhenPlayerIsReady()
        {
            for (int i = 0; i < cameraRebindRetries; i++)
            {
                yield return null;

                if (TryGetMainPlayer(out Character player))
                {
                    MMCameraEvent.Trigger(MMCameraEventTypes.SetTargetCharacter, player);
                    MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
                    DirectlyBindCorgiCameras(player);
                    yield break;
                }

                if (cameraRebindRetryDelay > 0f)
                {
                    yield return new WaitForSeconds(cameraRebindRetryDelay);
                }
            }
        }

        private bool TryGetMainPlayer(out Character player)
        {
            player = null;

            if (!LevelManager.HasInstance || LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0)
            {
                return false;
            }

            player = LevelManager.Instance.Players[0];
            return player != null;
        }

        private void DirectlyBindCorgiCameras(Character player)
        {
            CinemachineCameraController[] cameraControllers = FindObjectsByType<CinemachineCameraController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (CinemachineCameraController cameraController in cameraControllers)
            {
                if (cameraController == null)
                {
                    continue;
                }

                cameraController.SetTarget(player);
                cameraController.StartFollowing();
            }
        }
    }
}
