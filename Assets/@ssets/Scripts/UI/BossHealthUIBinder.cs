using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace MaskboundJinosi.UI
{
    [AddComponentMenu("Maskbound/UI/Boss Health UI Binder")]
    public class BossHealthUIBinder : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject rootToShowHide;
        [SerializeField] private bool hideWhenNoBoss = true;
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Displays")]
        [SerializeField] private HealthProgressBarDisplay healthBarDisplay;
        [SerializeField] private HealthTextDisplay healthTextDisplay;
        [SerializeField] private TMP_Text bossNameText;

        [Header("Debug")]
        [SerializeField] private bool logBinding;

        private BossHealthTarget _currentTarget;
        private float _nextScanAt;

        private void Reset()
        {
            rootToShowHide = gameObject;
            rootCanvasGroup = GetComponent<CanvasGroup>();
            healthBarDisplay = GetComponentInChildren<HealthProgressBarDisplay>(true);
            healthTextDisplay = GetComponentInChildren<HealthTextDisplay>(true);
            bossNameText = GetComponentInChildren<TMP_Text>(true);
        }

        private void Awake()
        {
            rootToShowHide ??= gameObject;
            if (rootToShowHide == gameObject)
            {
                rootCanvasGroup ??= GetComponent<CanvasGroup>();
                rootCanvasGroup ??= gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            BossHealthTarget.Registered += Bind;
            BossHealthTarget.Unregistered += Unbind;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            RefreshBinding();
        }

        private void OnDisable()
        {
            BossHealthTarget.Registered -= Bind;
            BossHealthTarget.Unregistered -= Unbind;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshBinding();
        }

        private void Update()
        {
            if (_currentTarget != null || Time.unscaledTime < _nextScanAt)
            {
                return;
            }

            _nextScanAt = Time.unscaledTime + 0.25f;
            RefreshBinding();
        }

        private void RefreshBinding()
        {
            BossHealthTarget target = BossHealthTarget.Current;
            if (target == null)
            {
                target = FindActiveBossHealthTarget();
            }

            if (target != null)
            {
                Bind(target);
            }
            else
            {
                Clear();
            }
        }

        private void Bind(BossHealthTarget target)
        {
            if (target == null || target.Health == null)
            {
                Clear();
                return;
            }

            _currentTarget = target;

            SetVisible(true);

            if (logBinding)
            {
                Debug.Log($"{name}: bind boss health '{target.DisplayName}'.", this);
            }

            if (healthBarDisplay != null)
            {
                healthBarDisplay.AutoFindPlayerHealth = false;
                healthBarDisplay.SetTargetHealth(target.Health);
            }

            if (healthTextDisplay != null)
            {
                healthTextDisplay.AutoFindPlayerHealth = false;
                healthTextDisplay.SetTargetHealth(target.Health);
            }

            if (bossNameText != null)
            {
                bossNameText.text = target.DisplayName;
            }
        }

        private void Unbind(BossHealthTarget target)
        {
            if (target == _currentTarget)
            {
                Clear();
            }
        }

        private void Clear()
        {
            _currentTarget = null;

            if (logBinding)
            {
                Debug.Log($"{name}: clear boss health binding.", this);
            }

            if (healthBarDisplay != null)
            {
                healthBarDisplay.SetTargetHealth(null);
            }

            if (healthTextDisplay != null)
            {
                healthTextDisplay.SetTargetHealth(null);
            }

            if (bossNameText != null)
            {
                bossNameText.text = string.Empty;
            }

            if (hideWhenNoBoss)
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool visible)
        {
            if (rootToShowHide == null)
            {
                return;
            }

            if (rootToShowHide == gameObject)
            {
                rootCanvasGroup ??= GetComponent<CanvasGroup>();
                rootCanvasGroup ??= gameObject.AddComponent<CanvasGroup>();
                rootCanvasGroup.alpha = visible ? 1f : 0f;
                rootCanvasGroup.interactable = visible;
                rootCanvasGroup.blocksRaycasts = visible;
                return;
            }

            rootToShowHide.SetActive(visible);
        }

        private BossHealthTarget FindActiveBossHealthTarget()
        {
            BossHealthTarget[] targets =
                Object.FindObjectsByType<BossHealthTarget>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && targets[i].Health != null)
                {
                    return targets[i];
                }
            }

            return null;
        }
    }
}
