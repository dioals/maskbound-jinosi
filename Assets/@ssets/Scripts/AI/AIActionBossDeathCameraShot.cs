using System.Collections;
using MaskboundJinosi.Gameplay;
using MaskboundJinosi.Gameplay.Scene;
using MaskboundJinosi.UI;
using MoreMountains.CorgiEngine;
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

        [Header("Optional Slow Motion")]
        [SerializeField] private bool useSlowMotion;
        [Range(0.05f, 1f)] [SerializeField] private float slowMotionScale = 0.35f;

        private Coroutine _shotRoutine;
        private float _previousTimeScale = 1f;
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
            if (shotDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(shotDelay);
            }

            FocusCharacter(bossCharacter);
            _cameraIsOnBoss = true;

            _previousTimeScale = Time.timeScale;
            if (hitStopDuration > 0f)
            {
                Time.timeScale = 0f;
                yield return new WaitForSecondsRealtime(hitStopDuration);
                Time.timeScale = _previousTimeScale;
            }

            if (useSlowMotion)
            {
                Time.timeScale = slowMotionScale;
            }

            if (deathAnimationDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(deathAnimationDuration);
            }

            RestoreTimeScale();

            DemoBossChallengeTimer timer = FindFirstObjectByType<DemoBossChallengeTimer>(FindObjectsInactive.Include);
            float completionTime = timer != null ? timer.ElapsedTime : 0f;
            bool rewardEligible = timer != null && timer.RewardEligible;
            BossVictoryOverlay.Show(completionTime, rewardEligible);

            // Lewati frame saat overlay dibuat agar input last-hit tidak ikut dianggap konfirmasi.
            yield return null;
            while (!OverlayConfirmInput.WasPressedThisFrame())
            {
                yield return null;
            }

            if (resetChallengeTimer)
            {
                timer?.ResetTimer();
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

        private void RestoreTimeScale()
        {
            if (!Mathf.Approximately(Time.timeScale, _previousTimeScale))
            {
                Time.timeScale = _previousTimeScale;
            }
        }
    }
}
