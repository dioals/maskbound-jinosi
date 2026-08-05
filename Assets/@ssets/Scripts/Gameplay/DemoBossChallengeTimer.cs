using MoreMountains.CorgiEngine;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace MaskboundJinosi.Gameplay
{
    [AddComponentMenu("Maskbound/Gameplay/Demo Boss Challenge Timer")]
    public class DemoBossChallengeTimer : MonoBehaviour
    {
        [Header("Boss")]
        [Tooltip("Health milik boss yang menghentikan timer saat mati.")]
        [SerializeField] private Health bossHealth;
        [Tooltip("Nama GameObject/root boss yang dicari otomatis setiap level dimuat.")]
        [SerializeField] private string bossObjectName = "PrabuKlana";
        [SerializeField] private bool autoFindBoss = true;
        [Min(0.1f)] [SerializeField] private float bossSearchInterval = 0.5f;

        [Header("Timer")]
        [Tooltip("Timer otomatis dimulai saat scene/gameplay mulai.")]
        [SerializeField] private bool startAutomatically = true;
        [Tooltip("Batas waktu agar player berhak menerima reward.")]
        [Min(0f)] [SerializeField] private float rewardTimeLimit = 60f;
        [Tooltip("Aktifkan jika timer harus tetap berjalan saat Time.timeScale bernilai 0.")]
        [SerializeField] private bool useUnscaledTime;

        [Header("UI (Optional)")]
        [SerializeField] private TMP_Text timerText;
        [Tooltip("Jika aktif, tampilkan sisa waktu reward. Biarkan nonaktif untuk stopwatch waktu penaklukan boss.")]
        [SerializeField] private bool displayCountdown;
        [SerializeField] private string timerFormat = "{0:00}:{1:00}";

        [Header("Events")]
        public UnityEvent OnTimerStarted;
        public UnityEvent OnRewardTimeExceeded;
        public UnityEvent OnBossDefeated;
        public UnityEvent OnRewardEarned;
        public UnityEvent OnRewardFailed;

        public float ElapsedTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool BossDefeated { get; private set; }
        public bool RewardEligible => BossDefeated && ElapsedTime <= rewardTimeLimit;

        private bool _limitEventInvoked;
        private float _nextBossSearchTime;

        private void Awake()
        {
            BindBossHealth(bossHealth);
            UpdateTimerText();
        }

        private void Start()
        {
            if (startAutomatically)
            {
                StartTimer();
            }
        }

        private void OnDestroy()
        {
            BindBossHealth(null);
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (autoFindBoss && bossHealth == null && Time.unscaledTime >= _nextBossSearchTime)
            {
                _nextBossSearchTime = Time.unscaledTime + bossSearchInterval;
                TryFindBoss();
            }

            ElapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (!_limitEventInvoked && rewardTimeLimit > 0f && ElapsedTime > rewardTimeLimit)
            {
                _limitEventInvoked = true;
                OnRewardTimeExceeded?.Invoke();
            }

            UpdateTimerText();
        }

        public void StartTimer()
        {
            ElapsedTime = 0f;
            BossDefeated = false;
            _limitEventInvoked = false;
            IsRunning = true;
            _nextBossSearchTime = 0f;
            if (autoFindBoss)
            {
                TryFindBoss();
            }
            UpdateTimerText();
            OnTimerStarted?.Invoke();
        }

        public void StopTimer()
        {
            IsRunning = false;
            UpdateTimerText();
        }

        public void ResetTimer()
        {
            IsRunning = false;
            ElapsedTime = 0f;
            BossDefeated = false;
            _limitEventInvoked = false;
            BindBossHealth(null);
            UpdateTimerText();
        }

        public void SetBossHealth(Health health)
        {
            BindBossHealth(health);
        }

        public bool TryFindBoss()
        {
            if (string.IsNullOrWhiteSpace(bossObjectName))
            {
                return false;
            }

            Health[] healthComponents = FindObjectsByType<Health>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Health health in healthComponents)
            {
                if (health == null)
                {
                    continue;
                }

                Transform root = health.transform.root;
                if (health.gameObject.name == bossObjectName ||
                    (root != null && root.name == bossObjectName))
                {
                    BindBossHealth(health);
                    return true;
                }
            }

            return false;
        }

        private void BindBossHealth(Health health)
        {
            if (bossHealth != null)
            {
                bossHealth.OnDeath -= HandleBossDeath;
            }

            bossHealth = health;

            if (bossHealth != null)
            {
                bossHealth.OnDeath -= HandleBossDeath;
                bossHealth.OnDeath += HandleBossDeath;
            }
        }

        private void HandleBossDeath()
        {
            if (BossDefeated)
            {
                return;
            }

            BossDefeated = true;
            StopTimer();
            OnBossDefeated?.Invoke();

            if (ElapsedTime <= rewardTimeLimit)
            {
                OnRewardEarned?.Invoke();
            }
            else
            {
                OnRewardFailed?.Invoke();
            }
        }

        private void UpdateTimerText()
        {
            if (timerText == null)
            {
                return;
            }

            float shownTime = displayCountdown
                ? Mathf.Max(0f, rewardTimeLimit - ElapsedTime)
                : ElapsedTime;
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(shownTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = string.Format(timerFormat, minutes, seconds);
        }
    }
}
