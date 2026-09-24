using System.Collections;
using MaskboundJinosi.UI;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace MaskboundJinosi.Gameplay.Scene
{
    [AddComponentMenu("Maskbound/Scene/Player Game Over Overlay Controller")]
    public class PlayerGameOverOverlayController : MonoBehaviour, MMEventListener<CorgiEngineEvent>
    {
        [SerializeField] private bool resetChallengeTimer = true;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private DemoBossChallengeTimer challengeTimer;

        private Coroutine _returnRoutine;

        private void Awake()
        {
            gameFlowManager ??= GetComponent<GameFlowManager>();
            challengeTimer ??= GetComponent<DemoBossChallengeTimer>();
        }

        private void OnEnable()
        {
            this.MMEventStartListening<CorgiEngineEvent>();
        }

        private void OnDisable()
        {
            this.MMEventStopListening<CorgiEngineEvent>();
        }

        public void OnMMEvent(CorgiEngineEvent engineEvent)
        {
            if (engineEvent.EventType != CorgiEngineEventTypes.GameOver || _returnRoutine != null)
            {
                return;
            }

            ShowGameOver();
        }

        /// <summary>
        /// Shows the game-over overlay. Public so it can be called from a
        /// Fungus "Call Method" command (target = this GameObject).
        /// </summary>
        public void ShowGameOverFromFungus()
        {
            ShowGameOver();
        }

        private void ShowGameOver()
        {
            if (_returnRoutine != null)
            {
                return;
            }

            if (challengeTimer != null)
            {
                challengeTimer.StopTimer();
            }

            GameOverOverlay.Show();
            _returnRoutine = StartCoroutine(ReturnToStartScreen());
        }

        private IEnumerator ReturnToStartScreen()
        {
            // Tunggu input baru setelah overlay muncul, bukan input dari frame kematian.
            yield return null;
            while (!OverlayConfirmInput.WasPressedThisFrame())
            {
                yield return null;
            }

            if (ShowCreditsThenReturn())
            {
                _returnRoutine = null;
                yield break;
            }

            if (resetChallengeTimer)
            {
                challengeTimer?.ResetTimer();
            }

            gameFlowManager?.ReturnToMainMenu();
            _returnRoutine = null;
        }

        private bool ShowCreditsThenReturn()
        {
            CreditPager credits = CreditPager.FindInScene();
            if (credits == null)
            {
                return false;
            }

            credits.OnFinished.RemoveListener(OnCreditsFinished);
            credits.OnFinished.AddListener(OnCreditsFinished);
            credits.ShowCredits();
            return true;
        }

        private void OnCreditsFinished()
        {
            CreditPager credits = CreditPager.FindInScene();
            if (credits != null)
            {
                credits.OnFinished.RemoveListener(OnCreditsFinished);
            }

            if (resetChallengeTimer)
            {
                challengeTimer?.ResetTimer();
            }

            gameFlowManager?.ReturnToMainMenu();
        }
    }
}
