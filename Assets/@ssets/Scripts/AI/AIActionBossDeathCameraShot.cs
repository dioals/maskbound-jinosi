using System.Collections;
using MaskboundJinosi.Gameplay;
using MaskboundJinosi.Gameplay.Scene;
using MaskboundJinosi.UI;
using MoreMountains.CorgiEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.AI
{
    [AddComponentMenu("Maskbound/AI/Actions/AI Action Boss Death Camera Shot")]
    public class AIActionBossDeathCameraShot : AIAction
    {
        [Header("Target")]
        [Tooltip("Optional. Jika kosong, Character boss dicari dari parent AI Action.")]
        [SerializeField] private Character bossCharacter;

        [Header("Shot")]
        [Min(0f)] [SerializeField] private float shotDelay;
        [Tooltip("Hit-stop sesaat setelah last hit terdeteksi, sebelum animasi death berjalan.")]
        [Min(0f)] [SerializeField] private float hitStopDuration = 0.15f;
        [Tooltip("Waktu untuk memainkan animasi death sebelum kembali ke Start Screen.")]
        [Min(0f)] [SerializeField] private float deathAnimationDuration = 3f;
        [SerializeField] private bool returnToPlayer = true;
        [SerializeField] private bool playOnlyOnce = true;

        [Header("After Death")]
        [SerializeField] private bool returnToStartScreen = true;
        [SerializeField] private bool resetChallengeTimer = true;
        [Tooltip("Kalau ON, credit tampil setelah input confirm boss-mati ditekan, lalu start screen dibuka setelah credit selesai.")]
        [SerializeField] private bool showCreditsOnBossDeath = true;
        [Tooltip("Nama root overlay credit di scene. Dipakai untuk lookup ImagePager yang benar (bukan tutorial).")]
        [SerializeField] private string creditsRootName = "CreditUI";

        [Header("Slow Motion")]
        [Tooltip("Slow-mo dramatis setelah hit-stop untuk menegaskan boss sudah kalah.")]
        [SerializeField] private bool useSlowMotion = true;
        [Range(0.05f, 1f)] [SerializeField] private float slowMotionScale = 0.2f;
        [Tooltip("Durasi (real-time) slow-mo ditahan di Slow Motion Scale.")]
        [Min(0f)] [SerializeField] private float slowMotionHoldDuration = 1.2f;
        [Tooltip("Durasi (real-time) transisi dari Slow Motion Scale kembali ke kecepatan normal.")]
        [Min(0f)] [SerializeField] private float slowMotionRecoverDuration = 1f;
        [SerializeField] private AnimationCurve slowMotionRecoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine _shotRoutine;
        private float _previousTimeScale = 1f;
        private float _baseFixedDeltaTime;
        private bool _hasPlayed;
        private bool _cameraIsOnBoss;

        protected override void Awake()
        {
            base.Awake();
            ResolveBoss();
        }

        public override void Initialization()
        {
            if (!ShouldInitialize)
            {
                return;
            }

            ResolveBoss();
            base.Initialization();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            ResolveBoss();

            if (_shotRoutine != null || (playOnlyOnce && _hasPlayed) || bossCharacter == null)
            {
                return;
            }

            _hasPlayed = true;
            _shotRoutine = StartCoroutine(PlayShot());
        }

        public override void PerformAction()
        {
            // Shot dimulai sekali saat masuk state, bukan setiap update AI Brain.
        }

        private void OnDisable()
        {
            if (_shotRoutine != null)
            {
                StopCoroutine(_shotRoutine);
                _shotRoutine = null;
            }

            RestoreTimeScale();
            if (_cameraIsOnBoss && returnToPlayer)
            {
                FocusPlayer();
            }

            _cameraIsOnBoss = false;
        }

        private IEnumerator PlayShot()
        {
            // The killing hit already triggered a freeze-frame (DamageOnTouch hitstop),
            // so Time.timeScale can be 0 the moment this coroutine starts. This death
            // sequence must run at normal speed (death animation, dialog, confirm), so
            // normalize to 1 first instead of capturing the frozen 0 and restoring to it.
            // Clearing the MMTimeManager stack also stops that pending freeze from expiring
            // mid-sequence and snapping time back to 1 over our hit-stop / slow-mo.
            if (MMTimeManager.HasInstance)
            {
                MMTimeScaleEvent.Reset();
            }

            Time.timeScale = 1f;
            _previousTimeScale = 1f;
            _baseFixedDeltaTime = Time.fixedDeltaTime;

            if (shotDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(shotDelay);
            }

            FocusCharacter(bossCharacter);
            _cameraIsOnBoss = true;

            if (hitStopDuration > 0f)
            {
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(hitStopDuration);
                Time.timeScale = _previousTimeScale;
            }

            float slowMotionDuration = 0f;
            if (useSlowMotion)
            {
                slowMotionDuration = slowMotionHoldDuration + slowMotionRecoverDuration;
                yield return PlaySlowMotion();
            }

            // Slow-mo berjalan di dalam deathAnimationDuration, sisanya ditunggu normal.
            float remainingDeathDuration = deathAnimationDuration - slowMotionDuration;
            if (remainingDeathDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingDeathDuration);
            }

            RestoreTimeScale();

            DemoBossChallengeTimer timer = FindFirstObjectByType<DemoBossChallengeTimer>(FindObjectsInactive.Include);

            // Lewati frame agar input last-hit tidak ikut dianggap konfirmasi.
            yield return null;
            while (!OverlayConfirmInput.WasPressedThisFrame())
            {
                yield return null;
            }

            if (resetChallengeTimer)
            {
                timer?.ResetTimer();
            }

            if (showCreditsOnBossDeath && TryShowCredits())
            {
                // ImagePager.OnFinished melanjutkan ke start screen / player.
                _shotRoutine = null;
                yield break;
            }

            if (returnToStartScreen)
            {
                GameFlowManager gameFlow = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
                if (gameFlow != null)
                {
                    gameFlow.ReturnToMainMenu();
                }
                else
                {
                    Debug.LogWarning("AIActionBossDeathCameraShot: GameFlowManager tidak ditemukan, tidak bisa kembali ke Start Screen.", this);
                }
            }
            else if (returnToPlayer)
            {
                FocusPlayer();
            }

            _cameraIsOnBoss = false;
            _shotRoutine = null;
        }

        private bool TryShowCredits()
        {
            ImagePager credits = ImagePager.FindByRootName(creditsRootName);
            if (credits == null)
            {
                return false;
            }

            credits.OnFinished.RemoveListener(OnCreditsFinished);
            credits.OnFinished.AddListener(OnCreditsFinished);
            credits.Show();
            return true;
        }

        private void OnCreditsFinished()
        {
            ImagePager credits = ImagePager.FindByRootName(creditsRootName);
            if (credits != null)
            {
                credits.OnFinished.RemoveListener(OnCreditsFinished);
            }

            if (returnToStartScreen)
            {
                GameFlowManager gameFlow = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
                if (gameFlow != null)
                {
                    gameFlow.ReturnToMainMenu();
                }
                else
                {
                    Debug.LogWarning("AIActionBossDeathCameraShot: GameFlowManager tidak ditemukan, tidak bisa kembali ke Start Screen.", this);
                }
            }
            else if (returnToPlayer)
            {
                FocusPlayer();
            }

            _cameraIsOnBoss = false;
        }

        private void ResolveBoss()
        {
            if (bossCharacter == null)
            {
                bossCharacter = GetComponentInParent<Character>();
            }
        }

        private static void FocusCharacter(Character target)
        {
            if (target == null)
            {
                return;
            }

            MMCameraEvent.Trigger(MMCameraEventTypes.SetTargetCharacter, target);
            MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
        }

        private static void FocusPlayer()
        {
            if (!LevelManager.HasInstance || LevelManager.Instance.Players == null ||
                LevelManager.Instance.Players.Count == 0)
            {
                return;
            }

            FocusCharacter(LevelManager.Instance.Players[0]);
        }

        private IEnumerator PlaySlowMotion()
        {
            // Time scale is re-applied every frame so a stray freeze-frame event handled by
            // MMTimeManager can't override the slow-mo for longer than a single frame.
            float elapsed = 0f;
            while (elapsed < slowMotionHoldDuration)
            {
                ApplyTimeScale(slowMotionScale);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            elapsed = 0f;
            while (elapsed < slowMotionRecoverDuration)
            {
                float progress = slowMotionRecoverCurve.Evaluate(elapsed / slowMotionRecoverDuration);
                ApplyTimeScale(Mathf.LerpUnclamped(slowMotionScale, 1f, progress));
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            ApplyTimeScale(1f);
        }

        private void ApplyTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;

            // Scale physics steps too, otherwise rigidbodies stutter visibly during slow-mo.
            if (_baseFixedDeltaTime > 0f && timeScale > 0f)
            {
                Time.fixedDeltaTime = _baseFixedDeltaTime * timeScale;
            }
        }

        private void RestoreTimeScale()
        {
            // Always restore to full speed: this is a death sequence that must finish
            // (animations, dialog, return to start). Comparing against _previousTimeScale
            // could skip the restore if that captured value was 0 from the killing hitstop.
            ApplyTimeScale(1f);
            _previousTimeScale = 1f;
        }
    }
}
