using MoreMountains.Tools;
using MoreMountains.CorgiEngine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaskboundJinosi.Gameplay.Scene
{
    [AddComponentMenu("Maskbound/Scene/Bootstrap Scene Loader")]
    public class BootstrapSceneLoader : MonoBehaviour
    {
        private static BootstrapSceneLoader _instance;
        private static bool _firstLevelLoaded;

        [Header("Persistent Root")]
        [SerializeField] private bool keepRootAlive = true;
        [SerializeField] private bool destroyDuplicateBootstrap = true;

        [Header("First Level")]
        [SerializeField] private bool loadFirstLevelOnStart = true;
        [SerializeField] private string firstLevelName = "Prototype_Level_01";

        [Header("Loading")]
        [SerializeField] private bool useCorgiLoadingScreen = true;
        [SerializeField] private string loadingSceneName = "LoadingScreen";

        [Header("Camera Target")]
        [SerializeField] private bool rebindCameraTargetAfterLevelLoad = true;
        [SerializeField] private int cameraRebindRetries = 20;
        [SerializeField] private float cameraRebindRetryDelay = 0.1f;

        public string FirstLevelName => firstLevelName;

        private void Awake()
        {
            if (destroyDuplicateBootstrap && _instance != null && _instance != this)
            {
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

            if (IsSceneLoaded(levelName))
            {
                return;
            }

            if (useCorgiLoadingScreen)
            {
                MMSceneLoadingManager.LoadScene(levelName, loadingSceneName);
                return;
            }

            SceneManager.LoadScene(levelName);
        }

        public void ResetFirstLevelLoadedFlag()
        {
            _firstLevelLoaded = false;
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
            if (!rebindCameraTargetAfterLevelLoad || scene.name == loadingSceneName)
            {
                return;
            }

            StartCoroutine(RebindCameraTargetWhenPlayerIsReady());
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
